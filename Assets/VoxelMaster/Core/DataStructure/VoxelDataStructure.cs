using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public abstract class VoxelDataStructure {
    public abstract void Init (Vector3Int size);
    public abstract void Traverse (Action<int, int, int, Voxel> function);
    public abstract void TraverseZYX (Action<int, int, int, Voxel> function);
    public abstract Voxel GetVoxel (Vector3Int coords);
    public abstract Voxel GetVoxel (int index);
    public abstract void SetVoxel (Vector3Int coords, Voxel voxel);

    public abstract void SetVoxels (Voxel[] voxels);
    public abstract Voxel[] ToArray ();

    public abstract Voxel[] ExtractRegion (BoundsInt bound);

    public abstract void Save (BufferedStream stream);
    // public abstract void Load ();

    public static List<Vector3Int> staticVoxels = new List<Vector3Int> ();
}