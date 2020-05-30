using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

[System.Serializable]
public abstract class VoxelDataStructure {
    public abstract void Init (Vector3Int size);
    public abstract void Traverse (Action<int, int, int, Voxel> function);
    public abstract void TraverseZYX (Action<int, int, int, Voxel> function);
    public abstract Voxel GetVoxel (int x, int y, int z);
    public abstract Voxel GetVoxel (int index);
    public abstract void SetVoxel (int x, int y, int z, Voxel voxel);
    public abstract void SetVoxel (int index, Voxel voxel);

    public abstract void SetVoxels (Voxel[][][] voxels);
    public abstract Voxel[] ToArray (int sizeX, int sizeY, int sizeZ);

    public abstract Voxel[][][] ExtractRegion (BoundsInt bound, Dictionary<Vector3Int, List<int>> labels, int labelFilter);

    public abstract void Save (BufferedStream stream);
    // public abstract void Load ();

    public static List<Vector3Int> staticVoxels = new List<Vector3Int> ();
}

[StructLayout (LayoutKind.Sequential), Serializable]
struct FileHeader {
    public Vector3Int chunkSize;
}