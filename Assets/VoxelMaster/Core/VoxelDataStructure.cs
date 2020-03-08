using System;
using System.IO;
using UnityEngine;

[System.Serializable]
public abstract class VoxelDataStructure {
    public abstract void Init (Vector3Int dimensions);
    public abstract void Traverse (Action<int, int, int, Voxel> function);
    public abstract Voxel GetVoxel (Vector3Int coords);
    public abstract void SetVoxel (Vector3Int coords, Voxel voxel);

    public abstract void Save (BufferedStream stream);
    // public abstract void Load ();
}