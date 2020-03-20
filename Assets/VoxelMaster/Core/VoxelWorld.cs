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
    public int generationRadius = 3;

    [HideInInspector] public String dataStructureType;
    [HideInInspector] public String meshGeneratorType;

    MeshCollider collider;

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

        // var chunksToGenerate = new Vector3Int (10, 1, 80);
        // for (int i = 0; i < chunksToGenerate.x * chunksToGenerate.y * chunksToGenerate.z; i++)
        //     AddChunk (Util.Map1DTo3D (i, chunksToGenerate));

    }

    void Update () {

        worldGenerator.MainThreadUpdate ();
        meshProvider.MainThreadUpdate ();

        RenderChunks ();
        // UpdateCollisionMeshes ();
        // if (!chunks.ContainsKey (Vector3Int.zero)) {
        //     AddChunk (new Vector3Int (0, 0, 0));
        // }
        // return;
        int targetChunkX = Mathf.RoundToInt (viewer.position.x / chunkSize);
        int targetChunkY = Mathf.RoundToInt (viewer.position.y / chunkSize);
        int targetChunkZ = Mathf.RoundToInt (viewer.position.z / chunkSize);

        var chunksToAdd = new List<Vector3Int> ();

        for (int zOffset = -generationRadius; zOffset < generationRadius; zOffset++)
            for (int yOffset = -generationRadius; yOffset < generationRadius; yOffset++)
                for (int xOffset = -generationRadius; xOffset < generationRadius; xOffset++) {
                    var chunkCoord = new Vector3Int (targetChunkX + xOffset, targetChunkY + yOffset, targetChunkZ + zOffset);
                    if (!chunks.ContainsKey (chunkCoord))
                        chunksToAdd.Add (chunkCoord);
                }

        chunksToAdd.OrderBy (coord => Vector3Int.Distance (new Vector3Int (targetChunkX, targetChunkY, targetChunkZ), coord))
            .ToList ().ForEach (coord => AddChunk (coord));

    }

    void RenderChunks () {
        foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunks) {
            var pos = new Vector3 (
                entry.Key.x * chunkSize,
                entry.Key.y * chunkSize,
                entry.Key.z * chunkSize
            );
            if (entry.Value.mesh != null)
                Graphics.DrawMesh (entry.Value.mesh, pos, Quaternion.identity, material, 0);
        }
    }

    void UpdateCollisionMeshes () {

        int targetChunkX = Mathf.RoundToInt (viewer.position.x / chunkSize);
        int targetChunkY = Mathf.RoundToInt (viewer.position.y / chunkSize);
        int targetChunkZ = Mathf.RoundToInt (viewer.position.z / chunkSize);

        var coord = new Vector3Int (targetChunkX, targetChunkY, targetChunkZ);
        if (chunks.ContainsKey (coord))
            collider.sharedMesh = chunks[coord].mesh;

    }

    private Voxel GetVoxel (Vector3Int coord) {
        var chunk = new Vector3Int (
            Mathf.FloorToInt (coord.x / (chunkSize - 1)),
            Mathf.FloorToInt (coord.y / (chunkSize - 1)),
            Mathf.FloorToInt (coord.z / (chunkSize - 1))
        );
        var voxelCoordInChunk = new Vector3Int (
            coord.x % (chunkSize - 1),
            coord.y % (chunkSize - 1),
            coord.z % (chunkSize - 1)
        );
        return chunks[chunk][voxelCoordInChunk];
    }

    public void AddChunk (Vector3Int pos) {
        var chunkVoxels = Util.CreateInstance<VoxelDataStructure> (dataStructureType);
        var chunk = new VoxelChunk (pos, chunkSize, voxelScale, chunkVoxels);
        chunk.voxelWorld = this;
        chunks.Add (pos, chunk);

        worldGenerator.RequestChunkData (chunk, OnChunkData);
    }

    private void OnChunkData (VoxelChunk chunk) {
        meshProvider.RequestChunkMesh (chunk, OnChunkMesh);
        // Debug.Log ("OnChunkData");
    }

    private void OnChunkMesh (ChunkMeshGenerationData chunkMeshGenerationData) {
        chunkMeshGenerationData.voxelChunk.GenerateMesh ();
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

    void OnDrawGizmos () {
        foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunks) {
            switch (entry.Value.status) {
                case ChunkStatus.Idle:
                    continue;
                case ChunkStatus.Loading:
                    Gizmos.color = Color.cyan;
                    break;
                default:
                    Gizmos.color = Color.gray;
                    break;
            }
            Gizmos.DrawWireCube (entry.Key * entry.Value.size, new Vector3 (entry.Value.size, entry.Value.size, entry.Value.size));

        }

        int targetChunkX = Mathf.RoundToInt (viewer.position.x / chunkSize);
        int targetChunkY = Mathf.RoundToInt (viewer.position.y / chunkSize);
        int targetChunkZ = Mathf.RoundToInt (viewer.position.z / chunkSize);

        Gizmos.color = new Color (1, 1, 1, .2f);
        Gizmos.DrawCube (new Vector3Int (targetChunkX, targetChunkY, targetChunkZ) * chunkSize, new Vector3 (chunkSize, chunkSize, chunkSize));
    }
}