using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class JaggedDataStructure : VoxelDataStructure {
    private Vector3Int size;
    private Voxel[][][] voxels;

    public override void Init (Vector3Int size) {
        this.size = size;
        this.voxels = new Voxel[size.x][][];
        for (int i = 0; i < voxels.Length; i++) {
            voxels[i] = new Voxel[size.y][];
            for (int j = 0; j < voxels[i].Length; j++) {
                voxels[i][j] = new Voxel[size.z];
            }
        }
    }

    public override Voxel GetVoxel (int x, int y, int z) {
        return voxels[x][y][z];
    }

    public override Voxel GetVoxel (int index) {
        var coord = Util.Map1DTo3D (index, size.x);
        return voxels[coord.x][coord.y][coord.z];
    }

    public override void SetVoxel (int x, int y, int z, Voxel voxel) {
        voxels[x][y][z] = voxel;
    }

    public override void SetVoxel (int index, Voxel voxel) {
        var coord = Util.Map1DTo3D (index, size.x);
        voxels[coord.x][coord.y][coord.z] = voxel;
    }

    public override void Traverse (Action<int, int, int, Voxel> function) {

        int sizeX = size.x;
        int sizeY = size.y;
        int sizeZ = size.z;

        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++) {
                    function.Invoke (x, y, z, voxels[x][y][z]);
                }
    }
    public override void TraverseZYX (Action<int, int, int, Voxel> function) {
        for (int z = 0; z < size.x; z++)
            for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.z; x++) {
                    function.Invoke (x, y, z, voxels[x][y][z]);
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
        var temp = new Voxel[size.x * size.y * size.z];
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.z; z++) {
                    var index = Util.Map3DTo1D (x, y, z, size.x);
                    temp[index] = voxels[x][y][z];
                }
        return temp;
    }

    public override void SetVoxels (Voxel[][][] voxels) {
        this.voxels = voxels;
    }

    public override Voxel[][][] ExtractRegion (BoundsInt bound, Dictionary<Vector3Int, List<int>> labels, int labelFilter) {
        Voxel[][][] region = new Voxel[(bound.size.x + 1)][][];
        for (int i = 0; i < region.Length; i++) {
            region[i] = new Voxel[bound.size.y + 1][];
            for (int j = 0; j < region[i].Length; j++) {
                region[i][j] = new Voxel[bound.size.z + 1];
            }
        }

        for (int x = bound.min.x; x <= bound.max.x; x++)
            for (int y = bound.min.y; y <= bound.max.y; y++)
                for (int z = bound.min.z; z <= bound.max.z; z++) {
                    var coords = new Vector3Int (x, y, z);

                    Voxel voxel;
                    if (((x <= bound.min.x || y <= bound.min.y || z <= bound.min.z) || (x >= bound.max.x || y >= bound.max.y || z >= bound.max.z)) ||
                        ((x <= 0 || y <= 0 || z <= 0) || (x >= size.x || y >= size.y || z >= size.z)) ||
                        (voxels[x][y][z].density >.0f && labels[coords].All (label => label != labelFilter))) {
                        voxel = new Voxel { density = -1 };
                    } else {
                        voxel = voxels[x][y][z];
                    }
                    region[x - bound.min.x][y - bound.min.y][z - bound.min.z] = voxel;
                    staticVoxels.Add (coords);
                }
        return region;
    }
}