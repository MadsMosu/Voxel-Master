using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class SimpleDataStructure : VoxelDataStructure {
    private int size;
    private Voxel[] voxels;

    public override void Init (int size) {
        this.size = size;
        this.voxels = new Voxel[size * size * size];
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
    public override void TraverseZYX (Action<int, int, int, Voxel> function) {
        for (int z = 0; z < size; z++)
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) {
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

}

[StructLayout (LayoutKind.Sequential), Serializable]
struct FileHeader {
    public int chunkSize;
}