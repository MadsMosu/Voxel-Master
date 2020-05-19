using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

// [ExecuteInEditMode]
public class VoxelWorld : MonoBehaviour {

    #region Parameters
    public float voxelScale = 1;
    public float isoLevel = 0;
    public Vector3Int chunkSize = new Vector3Int (32, 32, 32);
    #endregion

    private Dictionary<Vector3Int, VoxelChunk> chunks = new Dictionary<Vector3Int, VoxelChunk> ();
    private Dictionary<Vector3Int, Mesh> chunkMeshes = new Dictionary<Vector3Int, Mesh> ();
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

    private Queue<Vector3Int> generationQueue = new Queue<Vector3Int> ();
    private Thread generationThread;

    [HideInInspector] public String dataStructureType;
    [HideInInspector] public String meshGeneratorType;
    private VoxelMeshGenerator meshGenerator;

    #region Debug variables
    public Material material;
    #endregion

    Mesh mesh;
    VoxelChunk chunk;

    private float SignedDistanceSphere (Vector3 pos, Vector3 center, float radius) {
        return Vector3.Distance (pos, center) - radius;
    }

    private float SignedDistanceCube (Vector3 pos) {
        Vector3 p = pos - new Vector3 (16, 16, 16);
        Vector3 b = new Vector3 (12, 12, 12);
        Vector3 q = new Vector3 (Mathf.Abs (p.x) - b.x, Mathf.Abs (p.y) - b.y, Mathf.Abs (p.z) - b.z);
        float k = new Vector3 (Mathf.Max (q.x, 0f), Mathf.Max (q.y, 0f), Mathf.Max (q.z, 0f)).magnitude;
        float l = Mathf.Min (Mathf.Max (q.x, Mathf.Max (q.y, q.z)), 0f);
        return k + l;

    }

    private float SDFBox (Vector3 p, Vector3 c, Vector3 s) {
        float xx = Mathf.Max (
            p.x - c.x - new Vector3 (s.x / 2f, 0, 0).magnitude,
            c.x - p.x - new Vector3 (s.x / 2f, 0, 0).magnitude
        );

        float yx = Mathf.Max (
            p.y - c.y - new Vector3 (s.y / 2f, 0, 0).magnitude,
            c.y - p.y - new Vector3 (s.y / 2f, 0, 0).magnitude
        );

        float zx = Mathf.Max (
            p.z - c.z - new Vector3 (s.z / 2f, 0, 0).magnitude,
            c.z - p.z - new Vector3 (s.z / 2f, 0, 0).magnitude
        );

        float d = xx;
        d = Mathf.Max (d, yx);
        d = Mathf.Max (d, zx);
        return d;
    }
    void Start () {

        meshGenerator = Util.CreateInstance<VoxelMeshGenerator> (meshGeneratorType);

        // Debug.Log ("Creating chunks");

        // var chunksToGenerate = new Vector3Int (8, 1, 8);
        // for (int i = 0; i < chunksToGenerate.x * chunksToGenerate.y * chunksToGenerate.z; i++)
        //     AddChunk (Util.Map1DTo3D (i, chunksToGenerate));

        // generationThread = new Thread (ProcessGenerationQueue);
        // generationThread.Start ();
        VoxelDataStructure dataStructure = new SimpleDataStructure ();
        chunk = new VoxelChunk (chunkSize, voxelScale, isoLevel, dataStructure);
        dataStructure.Init (chunkSize);

        for (int x = 0; x < chunkSize.x; x++)
            for (int y = 0; y < chunkSize.y; y++)
                for (int z = 0; z < chunkSize.z; z++) {
                    chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = SDFBox (new Vector3 (x, y, z), new Vector3 (16, 16, 16), new Vector3 (12, 12, 12)) });
                }

        var meshData = meshGenerator.GenerateMesh (chunk, SignedDistanceCube);
        mesh = new Mesh ();
        mesh.SetVertices (meshData.vertices);
        mesh.SetTriangles (meshData.triangleIndicies, 0);
        mesh.RecalculateNormals ();

    }

    void Update () {
        if (mesh != null) {
            Graphics.DrawMesh (mesh, Vector3.zero, Quaternion.identity, material, 0);

        }

        // RenderChunks ();

        // foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunks) {
        //     var chunk = entry.Value;
        //     if (chunk.status == ChunkStatus.HasMeshData) {
        //         chunkMeshes.Add (entry.Key, chunk.BuildMesh ());
        //         return;
        //     }
        // }
    }

    void RenderChunks () {
        foreach (KeyValuePair<Vector3Int, Mesh> entry in chunkMeshes) {
            var pos = new Vector3 (
                entry.Key.x * chunkSize.x,
                entry.Key.y * chunkSize.y,
                entry.Key.z * chunkSize.z
            );
            Graphics.DrawMesh (entry.Value, pos, Quaternion.identity, material, 0);
        }
    }

    void ProcessGenerationQueue () {
        while (true) {
            if (generationQueue.Count > 0) {
                GenerateChunk (generationQueue.Dequeue ());
            }
            // Thread.Sleep (1);
        }
    }

    public void AddChunk (Vector3Int pos) {
        if (chunks.ContainsKey (pos)) return;
        chunks.Add (pos, new VoxelChunk (chunkSize, voxelScale, isoLevel, Util.CreateInstance<VoxelDataStructure> (dataStructureType)));
        generationQueue.Enqueue (pos);
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
    private void GenerateChunk (Vector3Int pos) {
        var chunk = chunks[pos];

        chunk.voxels.Init (chunk.size);
        var offset = new Vector3 (pos.x * chunkSize.x, pos.y * chunkSize.y, pos.z * chunkSize.z);

        chunk.voxels.Traverse (delegate (int x, int y, int z, Voxel v) {
            float voxelDensity = DensityFunction (new Vector3 (x, y, z) + offset);
            chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = voxelDensity });
        });

        chunk.GenerateMeshData (meshGenerator, DensityFunction);
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

    void OnDestroy () {
        generationThread?.Abort ();
    }

    void OnDrawGizmosSelected () {
        // foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunks) {
        //     switch (entry.Value.status) {
        //         case ChunkStatus.Idle:
        //             // Gizmos.color = Color.green;
        //             // Gizmos.DrawWireCube (entry.Key * entry.Value.size + entry.Value.size / 2, entry.Value.size);
        //             break;
        //         case ChunkStatus.Loading:
        //             Gizmos.color = Color.cyan;
        //             Gizmos.DrawWireCube (entry.Key * entry.Value.size + entry.Value.size / 2, entry.Value.size);
        //             break;
        //         default:
        //             Gizmos.color = Color.gray;
        //             Gizmos.DrawWireCube (entry.Key * entry.Value.size + entry.Value.size / 2, entry.Value.size);
        //             break;
        //     }
        // }
        Gizmos.color = Color.magenta;
        for (int x = 0; x < chunkSize.x; x++)
            for (int y = 0; y < chunkSize.y; y++)
                for (int z = 0; z < chunkSize.z; z++) {
                    Gizmos.DrawWireSphere (new Vector3 (x, y, z), 0.15f);
                }
    }

}