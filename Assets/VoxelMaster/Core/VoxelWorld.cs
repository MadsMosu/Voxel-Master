using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    private List<VoxelMaterial> _materials = new List<VoxelMaterial>();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial>(_materials); private set { _materials = value; } }

    public float voxelScale { get; private set; }
    public float isoLevel { get; private set; }

    public IEnumerable<VoxelDataStructure> dataStructures = Util.GetEnumerableOfType<VoxelDataStructure>();

    [HideInInspector]
    public int dataStructure;

    private Dictionary<Vector3Int, VoxelChunk> chunks = new Dictionary<Vector3Int, VoxelChunk>();

    void Start()
    {

    }

    void Update()
    {

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
