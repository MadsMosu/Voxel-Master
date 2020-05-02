using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class SimpleDataStructure : VoxelDataStructure {
    private Vector3Int size;
    private Voxel[] voxels;

    public override void Init (Vector3Int size) {
        this.size = size;
        this.voxels = new Voxel[size.x * size.y * size.z];
    }

    public override Voxel GetVoxel (Vector3Int coords) {
        return voxels[Util.Map3DTo1D (coords, size)];
    }

    public override Voxel GetVoxel (int index) {
        return voxels[index];
    }

    public override void SetVoxel (Vector3Int coords, Voxel voxel) {
        voxels[Util.Map3DTo1D (coords, size)] = voxel;
    }

    public override void Traverse (Action<int, int, int, Voxel> function) {

        for (int i = 0; i < voxels.Length; i++) {
            var coord = Util.Map1DTo3D (i, size);
            function.Invoke (coord.x, coord.y, coord.z, voxels[i]);
        }
    }
    public override void TraverseZYX (Action<int, int, int, Voxel> function) {
        for (int z = 0; z < size.x; z++)
            for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.z; x++) {
                    function.Invoke (x, y, z, voxels[Util.Map3DTo1D (new Vector3Int (x, y, z), size)]);
                }

    }

    public async override void Save (BufferedStream stream) {

        var header = new FileHeader {
            chunkSize = size
        };

        var formatter = new BinaryFormatter ();
        formatter.Serialize (stream, header);

    }

    public override Voxel[] ToArray () {
        return voxels;
    }

    public override void SetVoxels (Voxel[] voxels, Vector3Int size) {
        this.size = size;
        this.voxels = voxels;
    }

    public override Voxel[] ExtractRegion (BoundsInt bound) {
        Voxel[] region = new Voxel[bound.size.x * bound.size.y * bound.size.z];
        // Debug.Log (bound.min);
        // Debug.Log (bound.max);
        // Debug.Log (bound.center);
        for (int x = bound.min.x; x < bound.max.x; x++)
            for (int y = bound.min.y; y < bound.max.y; y++)
                for (int z = bound.min.z; z < bound.max.z; z++) {
                    Vector3Int coords = new Vector3Int (x, y, z);
                    // Debug.Log ($"coords: {coords}, 1DTo3D: {Util.Map1DTo3D (Util.Map3DTo1D (coords, size), size)}");
                    Voxel voxel;
                    if ((x < 0 || y < 0 || z < 0) || (x >= size.x || y >= size.y || z >= size.z)) {
                        voxel = new Voxel { density = 0, materialIndex = 0 };
                    } else {
                        voxel = voxels[Util.Map3DTo1D (coords, size)];
                    }
                    region[Util.Map3DTo1D (coords - bound.min, bound.size)] = voxel;
                    staticVoxels.Add (coords);
                }
        return region;
    }
}

[StructLayout (LayoutKind.Sequential), Serializable]
struct FileHeader {
    public Vector3Int chunkSize;
}