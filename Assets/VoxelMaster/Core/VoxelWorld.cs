using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using static ThreadedMeshProvider;

// [ExecuteInEditMode]
public class VoxelWorld : MonoBehaviour {

    #region Parameters
    public float voxelScale = 1;
    public float isoLevel = 0;
    public Vector3Int chunkSize = new Vector3Int (16, 16, 16);
    #endregion

    private WorldGenerator worldGenerator;
    private ThreadedMeshProvider meshProvider;
    private Dictionary<Vector3Int, VoxelChunk> chunks = new Dictionary<Vector3Int, VoxelChunk> ();
    private Dictionary<Vector3Int, Mesh> chunkMeshes = new Dictionary<Vector3Int, Mesh> ();
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

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

        meshProvider = new ThreadedMeshProvider (Util.CreateInstance<VoxelMeshGenerator> (meshGeneratorType));

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

        int targetChunkX = Mathf.RoundToInt (viewer.position.x / chunkSize.x);
        int targetChunkY = Mathf.RoundToInt (viewer.position.y / chunkSize.y);
        int targetChunkZ = Mathf.RoundToInt (viewer.position.z / chunkSize.z);

        var chunksToAdd = new List<Vector3Int> ();

        for (int zOffset = -generationRadius; zOffset < generationRadius; zOffset++)
            for (int yOffset = 0; yOffset < 4; yOffset++)
                for (int xOffset = -generationRadius; xOffset < generationRadius; xOffset++) {
                    var chunkCoord = new Vector3Int (targetChunkX + xOffset, yOffset, targetChunkZ + zOffset);
                    if (!chunks.ContainsKey (chunkCoord))
                        chunksToAdd.Add (chunkCoord);
                }

        chunksToAdd.OrderBy (coord => Vector3Int.Distance (new Vector3Int (targetChunkX, targetChunkY, targetChunkZ), coord))
            .ToList ().ForEach (coord => AddChunk (coord));

    }

    void RenderChunks () {
        foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunks) {
            var pos = new Vector3 (
                entry.Key.x * chunkSize.x,
                entry.Key.y * chunkSize.y,
                entry.Key.z * chunkSize.z
            );
            if (entry.Value.mesh != null)
                Graphics.DrawMesh (entry.Value.mesh, pos, Quaternion.identity, material, 0);
        }
    }

    void UpdateCollisionMeshes () {

        int targetChunkX = Mathf.RoundToInt (viewer.position.x / chunkSize.x);
        int targetChunkY = Mathf.RoundToInt (viewer.position.y / chunkSize.y);
        int targetChunkZ = Mathf.RoundToInt (viewer.position.z / chunkSize.z);

        var coord = new Vector3Int (targetChunkX, targetChunkY, targetChunkZ);
        if (chunks.ContainsKey (coord))
            collider.sharedMesh = chunks[coord].mesh;

    }

    public void AddChunk (Vector3Int pos) {
        var chunkVoxels = Util.CreateInstance<VoxelDataStructure> (dataStructureType);
        var chunk = new VoxelChunk (pos, chunkSize, voxelScale, isoLevel, chunkVoxels);
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

            Gizmos.DrawWireCube (entry.Key * entry.Value.size, entry.Value.size);

        }

        int targetChunkX = Mathf.RoundToInt (viewer.position.x / chunkSize.x);
        int targetChunkY = Mathf.RoundToInt (viewer.position.y / chunkSize.y);
        int targetChunkZ = Mathf.RoundToInt (viewer.position.z / chunkSize.z);

        Gizmos.color = new Color (1, 1, 1, .2f);
        Gizmos.DrawCube (new Vector3Int (targetChunkX, targetChunkY, targetChunkZ) * chunkSize, chunkSize);
    }
}