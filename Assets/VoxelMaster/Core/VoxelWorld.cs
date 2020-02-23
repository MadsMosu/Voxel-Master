using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


[ExecuteInEditMode]
public class VoxelWorld : MonoBehaviour
{

    #region Parameters
    public float voxelScale;
    public float isoLevel;
    public Vector3Int chunkSize;
    #endregion

    private Dictionary<Vector3Int, VoxelChunk> chunks = new Dictionary<Vector3Int, VoxelChunk>();
    private List<VoxelMaterial> _materials = new List<VoxelMaterial>();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial>(_materials); private set { _materials = value; } }


    [HideInInspector] public String dataStructureType;
    [HideInInspector] public String meshGeneratorType;

    #region Debug variables
    public Material material;
    #endregion

    private float SignedDistanceSphere(Vector3 pos, Vector3 center, float radius)
    {
        return Vector3.Distance(pos, center) - radius;
    }
    void Start()
    {


        Debug.Log("Generating chunks");
        for (int i = 0; i < 16 * 16; i++)
            AddChunk(Util.Map1DTo3D(i, Vector3Int.one * 16));

        var settings = new WorldGeneratorSettings
        {
            worldSize = 512
        };


    }

    void Update()
    {
        // if (mesh != null)
        // {
        //     // Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);

        // }
    }

    // public void GenerateTerrain(WorldGeneratorSettings settings, float amplifier = 1)
    // {
    //     var data = heightmapGenerator.Generate(settings);
    //     for (int x = 0; x < chunkSize.x; x++)
    //         for (int y = 0; y < chunkSize.y; y++)
    //             for (int z = 0; z < chunkSize.z; z++)
    //             {
    //                 float heightData = data[Util.Map2DTo1D(x, z, settings.worldSize)];
    //                 float baseHeight = 2.001f;
    //                 float desiredHeight = baseHeight + heightData * amplifier;
    //                 float baseDensity = desiredHeight / (y * 1.001f);
    //                 baseDensity += heightData;
    //                 dataStructure.SetVoxel(new Vector3Int(x, y, z), new Voxel
    //                 {
    //                     density = baseDensity
    //                 });
    //             }

    //     VoxelChunk chunk = new VoxelChunk(chunkSize, voxelScale, isoLevel, dataStructure);
    //     var meshData = meshGenerator.generateMesh(chunk);

    //     mesh = new Mesh();
    //     mesh.SetVertices(meshData.vertices);
    //     mesh.SetTriangles(meshData.triangleIndicies, 0);
    //     mesh.Optimize();
    //     mesh.RecalculateNormals();
    // }

    public void AddChunk(Vector3Int pos)
    {
        if (chunks.ContainsKey(pos)) return;

        var dataStructure = Util.CreateInstance<VoxelDataStructure>(dataStructureType);
        chunks.Add(pos, new VoxelChunk(chunkSize, voxelScale, isoLevel, dataStructure));
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
