using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelChunk
{
    private VoxelDataStructure _voxels;

    private List<VoxelMaterial> _materials = new List<VoxelMaterial>();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial>(_materials); private set { _materials = value; } }

    public Vector3 size { get; private set; }
    public float voxelScale { get; private set; }


    public void AddDensity(Vector3 pos, float[][][] densities)
    {
        throw new NotImplementedException();
    }

    public void SetDensity(Vector3 pos, float[][][] densities)
    {
        throw new NotImplementedException();
    }

    public void RemoveDensity(Vector3 pos, float[][][] densities)
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