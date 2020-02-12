using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    private List<VoxelMaterial> _materials = new List<VoxelMaterial>();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial>(_materials); private set { _materials = value; } }

    public float voxelScale;
    public float isoLevel;
    public Vector3Int chunkSize;
    public Material material;
    private Dictionary<Vector3Int, VoxelChunk> chunks = new Dictionary<Vector3Int, VoxelChunk>();
    private Dictionary<VoxelChunk, Mesh> chunkMeshes = new Dictionary<VoxelChunk, Mesh>();

    public IEnumerable<VoxelDataStructure> dataStructures = Util.GetEnumerableOfType<VoxelDataStructure>();
    [HideInInspector]
    public int dataStructureIndex = 0;

    public IEnumerable<VoxelMeshGenerator> meshGenerators = Util.GetEnumerableOfType<VoxelMeshGenerator>();
    [HideInInspector]
    public int meshGeneratorIndex = 0;

    private VoxelMeshGenerator meshGenerator;
    private VoxelDataStructure dataStructure;
    public Mesh mesh;



    private float SignedDistanceSphere(Vector3 pos, Vector3 center, float radius)
    {
        return Vector3.Distance(pos, center) - radius;
    }
    void Start()
    {
        dataStructure = dataStructures.Cast<VoxelDataStructure>().ElementAt(dataStructureIndex);
        dataStructure.Init(chunkSize);

        for (int x = 0; x < chunkSize.x - 1; x++)
            for (int y = 0; y < chunkSize.y - 1; y++)
                for (int z = 0; z < chunkSize.z - 1; z++)
                {
                    dataStructure.SetVoxel(new Vector3Int(x, y, z), new Voxel { density = SignedDistanceSphere(new Vector3(x, y, z), new Vector3(8, 8, 8), 6) });
                }

        meshGenerator = meshGenerators.Cast<VoxelMeshGenerator>().ElementAt(meshGeneratorIndex);
        VoxelChunk chunk = new VoxelChunk(chunkSize, voxelScale, isoLevel, dataStructure);
        var meshData = meshGenerator.generateMesh(chunk);

        mesh = new Mesh();
        mesh.SetVertices(meshData.vertices);
        mesh.SetTriangles(meshData.triangleIndicies, 0);
        mesh.RecalculateNormals();
    }

    void Update()
    {
        if (mesh != null)
        {
            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);

        }
    }


    public void AddChunk(Vector3Int pos)
    {
        throw new NotImplementedException();
    }
    public void RemoveChunk(Vector3Int pos)
    {
        throw new NotImplementedException();
    }
    public VoxelChunk GetChunk(Vector3Int pos)
    {
        throw new NotImplementedException();
    }
    public void AddDensity(Vector3Int pos, float[][][] densities)
    {
        throw new NotImplementedException();
    }
    public void SetDensity(Vector3Int pos, float[][][] densities)
    {
        throw new NotImplementedException();
    }
    public void RemoveDensity(Vector3Int pos, float[][][] densities)
    {
        throw new NotImplementedException();
    }
    public VoxelMaterial GetMaterial(Vector3 pos)
    {
        throw new NotImplementedException();
    }
    public void SetMaterial(Vector3 pos, byte materialIndex)
    {
        throw new NotImplementedException();
    }
    public void SetMaterialInRadius(Vector3 pos, float radius, byte materialIndex)
    {
        throw new NotImplementedException();
    }
}
