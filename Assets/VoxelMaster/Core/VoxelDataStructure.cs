using UnityEngine;

public abstract class VoxelDataStructure
{
    public abstract void Init(Vector3Int dimensions);
    public abstract Voxel GetVoxel(Vector3Int coords);
    public abstract void SetVoxel(Vector3Int coords, Voxel voxel);
}