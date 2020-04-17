using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using static ThreadedMeshProvider;

//[ExecuteInEditMode]
// [System.Serializable]
public class VoxelWorld : MonoBehaviour, IVoxelData {

    #region Parameters
    public float voxelScale = 1;
    public float isoLevel = 0;
    public int chunkSize = 16;
    #endregion

    private WorldGenerator worldGenerator;
    private MeshGeneratorSettings meshGeneratorSettings;
    private ThreadedMeshProvider meshProvider;
    // private Dictionary<Vector3Int, Mesh> chunkMeshes = new Dictionary<Vector3Int, Mesh> ();
    private List<VoxelChunk> dirtChunks = new List<VoxelChunk> ();
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

    private Dictionary<Vector3Int, VoxelChunk> chunks;
    private Octree chunksOctree;

    public Voxel this [Vector3 v] {
        get => this [(int) v.x, (int) v.y, (int) v.z];
        set => this [(int) v.x, (int) v.y, (int) v.z] = value;
    }
    public Voxel this [Vector3Int v] {
        get => GetVoxel (v);
        set => SetVoxel (v, value);
    }
    public Voxel this [int x, int y, int z] {
        get => this [new Vector3Int (x, y, z)];
        set => this [new Vector3Int (x, y, z)] = value;
    }

    private float viewDistance = 300;
    public Transform viewer;
    public Vector3Int viewerCoordinates;
    public int generationRadius = 3;

    [HideInInspector] public String dataStructureType;
    [HideInInspector] public String meshGeneratorType;

    new MeshCollider collider;

    #region Debug variables
    public Material material;
    #endregion

    private enum Sides {
        NegativeX = 0,
        PositiveX = 1,
        NegativeY = 2,
        PositiveY = 3,
        NegativeZ = 4,
        PositiveZ = 5
    }
    void OnEnable () {

        Debug.Log ("OnEnable");

        chunks = new Dictionary<Vector3Int, VoxelChunk> ();
        if (chunksOctree == null) chunksOctree = new Octree (16, 9);

        int cornerIndex = 0;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++) {
                    if (x == 0 && y == 0 && z == 0) continue;
                    neighbourOffsets[cornerIndex++] = new Vector3Int (x, y, z);
                }

        worldGenerator = new WorldGenerator (new WorldGeneratorSettings {
            baseHeight = 0,
                heightAmplifier = 10,
                noiseScale = 1f,
                seed = 45432345,
                voxelScale = voxelScale
        });

        meshGeneratorSettings = new MeshGeneratorSettings {
            chunkSize = chunkSize,
            voxelScale = voxelScale,
            isoLevel = isoLevel
        };

        meshProvider = new ThreadedMeshProvider (this, Util.CreateInstance<VoxelMeshGenerator> (meshGeneratorType), meshGeneratorSettings);

        ExpandChunkGeneration ();
        UpdateViewerCoordinates ();

    }

    void OnDisable () {
        transform.GetComponentsInChildren<MeshCollider> ().ToList ().ForEach (col => DestroyImmediate (col.gameObject));
    }

    Vector3Int[] neighbourOffsets = new Vector3Int[26];
    void Start () {

        UpdateViewerCoordinates ();
        AddChunk (viewerCoordinates);
        ExpandChunkGeneration ();

    }

    bool viewerCoordinatesChanged = false;
    void UpdateViewerCoordinates () {
        viewerCoordinatesChanged = false;
        int targetChunkX = Int_floor_division ((int) viewer.position.x, chunkSize);
        int targetChunkY = Int_floor_division ((int) viewer.position.y, chunkSize);
        int targetChunkZ = Int_floor_division ((int) viewer.position.z, chunkSize);
        var newViewerCoordinates = new Vector3Int (targetChunkX, targetChunkY, targetChunkZ);
        if (newViewerCoordinates != viewerCoordinates) viewerCoordinatesChanged = true;
        viewerCoordinates = newViewerCoordinates;
    }

    void Update () {

        UpdateViewerCoordinates ();
        if (viewerCoordinatesChanged) {
            // ExpandChunkGeneration ();
            // AddChunk (viewerCoordinates);
            GenerateTerrainMeshes ();
        }

        worldGenerator.MainThreadUpdate ();
        meshProvider.MainThreadUpdate ();

        RenderChunks ();
        // UpdateCollisionMeshes ();
        // if (!chunks.ContainsKey (Vector3Int.zero)) {
        //     AddChunk (new Vector3Int (0, 0, 0));
        // }
        // return;

    }

    HashSet<uint> lod0Nodes = new HashSet<uint> ();
    HashSet<uint> lod1Nodes = new HashSet<uint> ();
    HashSet<uint> lod2Nodes = new HashSet<uint> ();
    private void GenerateTerrainMeshes () {
        previewMeshes.Clear ();
        lod0Nodes.Clear ();
        lod1Nodes.Clear ();
        lod2Nodes.Clear ();

        var currentNodeLocation = chunksOctree.GetNodeIndexAtCoord (viewerCoordinates);
        lod0Nodes.Add (currentNodeLocation);
        lod0Nodes.Add (currentNodeLocation >> 3);

        for (int x = -2; x <= 2; x++)
            for (int y = -2; y <= 2; y++)
                for (int z = -2; z <= 2; z++) {
                    if (x == 0 && y == 0 && z == 0) continue;
                    lod0Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation, new Vector3Int (x, y, z)));
                    lod1Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation >> 3, new Vector3Int (x, y, z)));
                    lod2Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation >> 6, new Vector3Int (x, y, z)));
                }

        lod0Nodes.ToList ().ForEach (code => {
            var node = chunksOctree.GetNode (code);
            meshProvider.RequestChunkMesh (new MeshGenerationRequest {
                origin = Util.FloorVector3 (node.bounds.min),
                    locationCode = code,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1 << 0
            });
        });

        lod1Nodes.ToList ().ForEach (code => {
            var node = chunksOctree.GetNode (code);
            Debug.Assert (node.chunk == null);
            meshProvider.RequestChunkMesh (new MeshGenerationRequest {
                origin = Util.FloorVector3 (node.bounds.min),
                    locationCode = code,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1 << 1
            });
        });

        lod2Nodes.ToList ().ForEach (code => {
            var node = chunksOctree.GetNode (code);
            Debug.Assert (node.chunk == null);
            meshProvider.RequestChunkMesh (new MeshGenerationRequest {
                origin = Util.FloorVector3 (node.bounds.min),
                    locationCode = code,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1 << 2
            });
        });
        return;
    }

    private Dictionary<uint, Mesh> previewMeshes = new Dictionary<uint, Mesh> ();
    private Dictionary<uint, MeshCollider> colliders = new Dictionary<uint, MeshCollider> ();
    private void OnChunkMesh (MeshGenerationResult res) {
        var mesh = res.meshData.BuildMesh ();
        previewMeshes[res.locationCode] = mesh;

        MeshCollider collider;
        if (colliders.ContainsKey (res.locationCode))
            collider = colliders[res.locationCode];
        else
            colliders[res.locationCode] = new GameObject ().AddComponent<MeshCollider> ();

        colliders[res.locationCode].sharedMesh = mesh;
        colliders[res.locationCode].transform.parent = transform;
        colliders[res.locationCode].transform.position = chunksOctree.GetNode (res.locationCode).bounds.min;

    }

    private void ExpandChunkGeneration () {
        for (int z = -generationRadius / 2; z < generationRadius / 2; z++)
            for (int y = -generationRadius / 2; y < generationRadius / 2; y++)
                for (int x = -generationRadius / 2; x < generationRadius / 2; x++) {
                    AddChunk (viewerCoordinates + new Vector3Int (x, y, z));
                }
    }

    void RenderChunks () {
        foreach (KeyValuePair<uint, Mesh> entry in previewMeshes) {
            var meshNode = chunksOctree.GetNode (entry.Key);
            Graphics.DrawMesh (entry.Value, meshNode.bounds.min, Quaternion.identity, material, 0);

        }
    }
    private int Int_floor_division (int value, int divider) {
        int q = value / divider;
        if (value % divider < 0) return q - 1;
        else return q;
    }

    private Voxel GetVoxel (Vector3Int coord) {

        var chunkCoord = new Vector3Int (
            Int_floor_division (coord.x, (chunkSize - 1)),
            Int_floor_division (coord.y, (chunkSize - 1)),
            Int_floor_division (coord.z, (chunkSize - 1))
        );
        var voxelCoordInChunk = new Vector3Int (
            coord.x % (chunkSize - 1),
            coord.y % (chunkSize - 1),
            coord.z % (chunkSize - 1)
        );

        if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize - 1;
        if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize - 1;
        if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize - 1;

        if (!chunks.ContainsKey (chunkCoord)) return new Voxel { density = 0 };

        return chunks[chunkCoord][voxelCoordInChunk];
    }
    private void SetVoxel (Vector3Int coord, Voxel voxel) {
        var chunk = new Vector3Int (
            Int_floor_division (coord.x, (chunkSize - 1)),
            Int_floor_division (coord.y, (chunkSize - 1)),
            Int_floor_division (coord.z, (chunkSize - 1))
        );
        var voxelCoordInChunk = new Vector3Int (
            coord.x % (chunkSize - 1),
            coord.y % (chunkSize - 1),
            coord.z % (chunkSize - 1)
        );

        if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize - 1;
        if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize - 1;
        if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize - 1;

        if (!chunks.ContainsKey (chunk)) throw new IndexOutOfRangeException ();
        chunks[chunk][voxelCoordInChunk] = voxel;
    }

    public void AddChunk (Vector3Int pos) {
        if (chunks.ContainsKey (pos)) return;
        var chunkVoxels = Util.CreateInstance<VoxelDataStructure> (dataStructureType);
        var chunk = new VoxelChunk (pos, chunkSize, voxelScale, chunkVoxels);
        chunk.voxelWorld = this;

        chunks.Add (pos, chunk);
        chunksOctree.AddChunk (pos, chunk);

        worldGenerator.RequestChunkData (chunk, OnChunkData);
    }

    private void OnChunkData (VoxelChunk chunk) {
        chunk.setHasData ();

    }

    public void RemoveChunk (Vector3Int pos) {
        throw new NotImplementedException ();
    }
    public VoxelChunk GetChunk (Vector3Int pos) {
        throw new NotImplementedException ();
    }
    public void AddDensity (Vector3Int pos, float[][][] densities) {
        throw new NotImplementedException ();
    }
    public void SetDensity (Vector3Int pos, float[][][] densities) {
        throw new NotImplementedException ();
    }
    public void RemoveDensity (Vector3Int pos, float[][][] densities) {
        throw new NotImplementedException ();
    }
    public VoxelMaterial GetMaterial (Vector3 pos) {
        throw new NotImplementedException ();
    }
    public void SetMaterial (Vector3 pos, byte materialIndex) {
        throw new NotImplementedException ();
    }
    public void SetMaterialInRadius (Vector3 pos, float radius, byte materialIndex) {
        throw new NotImplementedException ();
    }

    void DrawNodeIfExists (uint location, bool solid = false) {
        var positiveNeighbour = chunksOctree.GetNode (location);
        if (positiveNeighbour != null)
            if (solid)
                Gizmos.DrawCube (positiveNeighbour.bounds.center, positiveNeighbour.bounds.size);
            else
                Gizmos.DrawWireCube (positiveNeighbour.bounds.center, positiveNeighbour.bounds.size);
    }

    void OnDrawGizmos () {
        if (chunks == null) return;

        var currentNodeLocation = chunksOctree.GetNodeIndexAtCoord (viewerCoordinates);
        Gizmos.color = Color.red;
        DrawNodeIfExists (currentNodeLocation, true);

        lod0Nodes.ToList ().ForEach (n => {
            Gizmos.color = new Color (.5f, .5f, 1f, 1f);
            DrawNodeIfExists (n);
            Gizmos.color = new Color (.5f, .5f, 1f, .2f);
            // DrawNodeIfExists (n, true);
        });
        lod1Nodes.ToList ().ForEach (n => {
            Gizmos.color = new Color (1f, 1f, .2f, 1f);
            DrawNodeIfExists (n);
            Gizmos.color = new Color (1f, 1f, .2f, .2f);
            // DrawNodeIfExists (n, true);
        });
        lod2Nodes.ToList ().ForEach (n => {
            Gizmos.color = new Color (1f, .8f, .5f, 1f);
            DrawNodeIfExists (n);
            Gizmos.color = new Color (1f, .8f, .5f, .2f);
            // DrawNodeIfExists (n, true);
        });

        // for (int i = 0; i < neighbourOffsets.Length; i++) {
        //     var neighbourLocation = Octree.RelativeLeafNodeLocation (currentNodeLocation >> 6, neighbourOffsets[i]);
        //     Gizmos.color = new Color (.5f, .5f, 1f, 1f);
        //     DrawNodeIfExists (neighbourLocation);
        //     Gizmos.color = new Color (.5f, .5f, 1f, .5f);
        //     DrawNodeIfExists (neighbourLocation, true);
        // }
        // for (int i = 0; i < neighbourOffsets.Length; i++) {
        //     var neighbourLocation = Octree.RelativeLeafNodeLocation (currentNodeLocation >> 9, neighbourOffsets[i]);
        //     Gizmos.color = new Color (1f, .8f, .5f, 1f);
        //     DrawNodeIfExists (neighbourLocation);
        //     Gizmos.color = new Color (2f, .8f, .5f, .5f);
        //     DrawNodeIfExists (neighbourLocation, true);
        // }

    }
}