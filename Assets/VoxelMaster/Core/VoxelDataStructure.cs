using System;
using UnityEngine;

[System.Serializable]
public abstract class VoxelDataStructure {
    public abstract void Init (Vector3Int dimensions);
    public abstract void Traverse (Action<int, int, int, Voxel> function);
    public abstract Voxel GetVoxel (Vector3Int coords);
    public abstract void SetVoxel (Vector3Int coords, Voxel voxel);
}