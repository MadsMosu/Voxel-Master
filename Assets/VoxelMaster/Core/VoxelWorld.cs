using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using static ThreadedMeshProvider;

//[ExecuteInEditMode]
[System.Serializable]
public class VoxelWorld : MonoBehaviour, IVoxelData {

    #region Parameters
    public float voxelScale = 1;
    public float isoLevel = 0;
    public int chunkSize = 16;
    #endregion

    private WorldGenerator worldGenerator;
    private MeshGeneratorSettings meshGeneratorSettings;
    private ThreadedMeshProvider meshProvider;
    private Octree chunks;
    // private Dictionary<Vector3Int, Mesh> chunkMeshes = new Dictionary<Vector3Int, Mesh> ();
    private List<VoxelChunk> dirtChunks = new List<VoxelChunk> ();
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

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

        if (chunks == null) chunks = new Octree (16, 9);

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

        meshProvider = new ThreadedMeshProvider (Util.CreateInstance<VoxelMeshGenerator> (meshGeneratorType), meshGeneratorSettings);

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

        var currentNodeLocation = chunks.GetNodeIndexAtCoord (viewerCoordinates);
        lod0Nodes.Add (currentNodeLocation);
        lod2Nodes.Add (currentNodeLocation >> 9);

        for (int i = 0; i < neighbourOffsets.Length; i++) {
            lod0Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation, neighbourOffsets[i]));
            lod1Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation >> 6, neighbourOffsets[i]));
            lod2Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation >> 9, neighbourOffsets[i]));
        }

        lod0Nodes.ToList ().ForEach (code => {
            var nodeVoxels = chunks.ExtractVoxels (code, 0);
            meshProvider.RequestChunkMesh (new MeshGenerationRequest {
                locationCode = code,
                    voxels = nodeVoxels,
                    size = chunkSize + 3,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1 << 0
            });
        });
        lod1Nodes.ToList ().ForEach (code => {
            var nodeVoxels = chunks.ExtractVoxels (code, 1);
            meshProvider.RequestChunkMesh (new MeshGenerationRequest {
                locationCode = code,
                    voxels = nodeVoxels,
                    size = chunkSize + 3,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1 << 1
            });
        });
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
        colliders[res.locationCode].transform.position = chunks.GetNode (res.locationCode).bounds.min;

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
            var meshNode = chunks.GetNode (entry.Key);
            Graphics.DrawMesh (entry.Value, meshNode.bounds.min, Quaternion.identity, material, 0);

        }
    }
    private int Int_floor_division (int value, int divider) {
        int q = value / divider;
        if (value % divider < 0) return q - 1;
        else return q;
    }

    private Voxel GetVoxel (Vector3Int coord) {

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

        if (chunks.GetChunkAtCoord (chunk) == null) return new Voxel { density = 0 };

        return chunks.GetChunkAtCoord (chunk) [voxelCoordInChunk];
    }
    private void SetVoxel (Vector3Int coord, Voxel voxel) {
        // var chunk = new Vector3Int (
        //     Int_floor_division (coord.x, (chunkSize - 1)),
        //     Int_floor_division (coord.y, (chunkSize - 1)),
        //     Int_floor_division (coord.z, (chunkSize - 1))
        // );
        // var voxelCoordInChunk = new Vector3Int (
        //     coord.x % (chunkSize - 1),
        //     coord.y % (chunkSize - 1),
        //     coord.z % (chunkSize - 1)
        // );

        // if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize - 1;
        // if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize - 1;
        // if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize - 1;

        // if (!chunks.ContainsKey (chunk)) throw new IndexOutOfRangeException ();
        // chunks[chunk][voxelCoordInChunk] = voxel;
    }

    public void AddChunk (Vector3Int pos) {
        if (chunks.GetNodeIndexAtCoord (pos) != 0) return;
        var chunkVoxels = Util.CreateInstance<VoxelDataStructure> (dataStructureType);
        var chunk = new VoxelChunk (pos, chunkSize, voxelScale, chunkVoxels);
        chunk.voxelWorld = this;
        chunks.AddChunk (pos, chunk);

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

    void DrawNodeIfExists (uint location) {
        var positiveNeighbour = chunks.GetNode (location);
        if (positiveNeighbour != null)
            Gizmos.DrawWireCube (positiveNeighbour.bounds.center, positiveNeighbour.bounds.size);
    }

    void OnDrawGizmos () {
        if (chunks == null) return;

        // chunks.GetLeafChildren (0b1).ForEach (n => {
        //     Gizmos.color = n.chunk.hasData ? Color.green : Color.clear;
        //     Gizmos.DrawWireCube (n.bounds.center, n.bounds.extents);
        // });

        Gizmos.color = new Color (1, .3f, .2f, 1f);
        lod0Nodes.ToList ().ForEach (n => DrawNodeIfExists (n));
        Gizmos.color = new Color (.6f, 1f, .4f, .2f);
        lod1Nodes.ToList ().ForEach (n => DrawNodeIfExists (n));
        Gizmos.color = new Color (.7f, .7f, 1f, .1f);
        lod2Nodes.ToList ().ForEach (n => DrawNodeIfExists (n));
    }
}