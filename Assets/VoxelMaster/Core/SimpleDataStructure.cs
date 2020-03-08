using System;
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

    public override void SetVoxel (Vector3Int coords, Voxel voxel) {
        voxels[Util.Map3DTo1D (coords, size)] = voxel;
    }

    public override void Traverse (Action<int, int, int, Voxel> function) {

        for (int i = 0; i < voxels.Length; i++) {
            var coord = Util.Map1DTo3D (i, size);
            function.Invoke (coord.x, coord.y, coord.z, voxels[i]);
        }
    }

    public async override void Save (BufferedStream stream) {

        var header = new FileHeader {
            chunkSize = size
        };

        var formatter = new BinaryFormatter ();
        formatter.Serialize (stream, header);

    }
}

[StructLayout (LayoutKind.Sequential), Serializable]
struct FileHeader {
    public Vector3Int chunkSize;
}