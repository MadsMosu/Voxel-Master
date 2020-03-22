using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using static ThreadedMeshProvider;

// [ExecuteInEditMode]
public class VoxelWorld : MonoBehaviour, IVoxelData {

    #region Parameters
    public float voxelScale = 1;
    public float isoLevel = 0;
    public int chunkSize = 16;
    #endregion

    private WorldGenerator worldGenerator;
    private MeshGeneratorSettings meshGeneratorSettings;
    private ThreadedMeshProvider meshProvider;
    private Dictionary<Vector3Int, VoxelChunk> chunks = new Dictionary<Vector3Int, VoxelChunk> ();
    private Dictionary<Vector3Int, Mesh> chunkMeshes = new Dictionary<Vector3Int, Mesh> ();
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

    public Voxel this [Vector3 v] {
        get => this [(int) v.x, (int) v.y, (int) v.z];
        set => this [(int) v.x, (int) v.y, (int) v.z] = value;
    }
    public Voxel this [Vector3Int v] {
        get => GetVoxel (v);
        set =>
            throw new NotImplementedException ();
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

    private float SignedDistanceSphere (Vector3 pos, Vector3 center, float radius) {
        return Vector3.Distance (pos, center) - radius;
    }
    void Start () {

        worldGenerator = new WorldGenerator (new WorldGeneratorSettings {
            baseHeight = 0,
                heightAmplifier = 20,
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
        Debug.Log ("Creating chunks");

        collider = new GameObject ("Terrain collider").AddComponent<MeshCollider> ();

        UpdateViewerCoordinates ();
        var chunkCoord = viewerCoordinates;
        AddChunk (chunkCoord);
    }

    void UpdateViewerCoordinates () {
        int targetChunkX = Int_floor_division ((int) viewer.position.x, chunkSize);
        int targetChunkY = Int_floor_division ((int) viewer.position.y, chunkSize);
        int targetChunkZ = Int_floor_division ((int) viewer.position.z, chunkSize);
        viewerCoordinates = new Vector3Int (targetChunkX, targetChunkY, targetChunkZ);
    }

    void Update () {

        UpdateViewerCoordinates ();

        worldGenerator.MainThreadUpdate ();
        meshProvider.MainThreadUpdate ();

        RenderChunks ();
        // UpdateCollisionMeshes ();
        // if (!chunks.ContainsKey (Vector3Int.zero)) {
        //     AddChunk (new Vector3Int (0, 0, 0));
        // }
        // return;

        ExpandChunkGeneration ();

    }

    private void ExpandChunkGeneration () {
        foreach (var chunk in chunks.Values.ToArray ()) {
            for (int z = -1; z <= 1; z++)
                for (int y = -1; y <= 1; y++)
                    for (int x = -1; x <= 1; x++) {
                        if (x == 0 && y == 0 && z == 0) continue;
                        var neighbourCoords = new Vector3Int (
                            chunk.coords.x + x,
                            chunk.coords.y + y,
                            chunk.coords.z + z
                        );
                        if (!chunks.ContainsKey (neighbourCoords) &&
                            Vector3Int.Distance (viewerCoordinates, neighbourCoords) <= generationRadius) {
                            AddChunk (neighbourCoords);
                        }
                    }
        }
    }

    void RenderChunks () {
        foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunks) {
            var pos = new Vector3 (
                entry.Key.x * chunkSize,
                entry.Key.y * chunkSize,
                entry.Key.z * chunkSize
            );
            if (entry.Value.GetCurrentMesh () != null)
                Graphics.DrawMesh (entry.Value.GetCurrentMesh (), pos, Quaternion.identity, material, 0);
        }
    }

    void UpdateCollisionMeshes () {

        int targetChunkX = Int_floor_division ((int) viewer.position.x, chunkSize);
        int targetChunkY = Int_floor_division ((int) viewer.position.y, chunkSize);
        int targetChunkZ = Int_floor_division ((int) viewer.position.z, chunkSize);

        var coord = new Vector3Int (targetChunkX, targetChunkY, targetChunkZ);
        if (chunks.ContainsKey (coord))
            collider.sharedMesh = chunks[coord].GetCurrentMesh ();

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

        if (!chunks.ContainsKey (chunk)) return new Voxel ();
        return chunks[chunk][voxelCoordInChunk];
    }

    public void AddChunk (Vector3Int pos) {
        if (chunks.ContainsKey (pos)) return;
        var chunkVoxels = Util.CreateInstance<VoxelDataStructure> (dataStructureType);
        var chunk = new VoxelChunk (pos, chunkSize, voxelScale, chunkVoxels, meshProvider);
        chunk.voxelWorld = this;
        chunks.Add (pos, chunk);

        worldGenerator.RequestChunkData (chunk, OnChunkData);
    }

    private void OnChunkData (VoxelChunk chunk) {
        chunk.status = ChunkStatus.HasData;
        chunk.SetLod (UnityEngine.Random.Range (0, 2));
        // Debug.Log ("OnChunkData");
    }

    private void OnChunkMesh (ChunkMeshGenerationData chunkMeshGenerationData) {
        chunkMeshGenerationData.voxelChunk.status = ChunkStatus.Idle;
        // Debug.Log ("OnChunkMesh");
    }

    FastNoise noise = new FastNoise (34535284);

    private float DensityFunction (Vector3 pos) {

        var scale = .5f;

        float baseHeight = 0;
        float heightAmplifier = 20;

        float height = baseHeight + Mathf.Pow ((noise.GetCubicFractal ((pos.x) / scale, (pos.z) / scale) + .5f) * heightAmplifier, 1.6f);
        float voxelDensity = ((pos.y) - height) / heightAmplifier;
        return voxelDensity;
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

    Color idleColor = new Color (1, 1, 1, .05f);
    void OnDrawGizmos () {
        foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunks) {
            switch (entry.Value.status) {

                case ChunkStatus.HasData:
                    Gizmos.color = Color.yellow;
                    break;
                case ChunkStatus.HasMeshData:
                    Gizmos.color = Color.blue;
                    break;
                case ChunkStatus.GeneratingMesh:
                    Gizmos.color = Color.yellow;
                    break;
                default:
                    Gizmos.color = Color.clear;
                    break;
            }
            Gizmos.DrawWireCube (entry.Key * entry.Value.size, new Vector3 (entry.Value.size, entry.Value.size, entry.Value.size));

        }

        Gizmos.color = new Color (1, 1, 1, .2f);
        Gizmos.DrawCube (viewerCoordinates * chunkSize + (Vector3.one * chunkSize / 2f), new Vector3 (chunkSize, chunkSize, chunkSize));
    }
}