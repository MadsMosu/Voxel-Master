using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelMaster.Chunk;

public class TestVoxelWorld : IVoxelData {

    public Dictionary<Vector3Int, VoxelChunk> chunkDictionary = new Dictionary<Vector3Int, VoxelChunk> ();
    public int chunkSize;
    public Voxel this [Vector3 v] {
        get => this [(int) v.x, (int) v.y, (int) v.z];
        set => this [(int) v.x, (int) v.y, (int) v.z] = value;
    }
    public Voxel this [Vector3Int v] {
        get => GetVoxel (v);
        set => SetVoxel (v, value);
    }
    public Voxel this [int x, int y, int z] {
        get => this [new Vector3Int (x, y, z)];
        set => this [new Vector3Int (x, y, z)] = value;
    }

    private Voxel GetVoxel (Vector3Int coord) {
        var chunkCoord = new Vector3Int (
            Util.Int_floor_division (coord.x, (chunkSize)),
            Util.Int_floor_division (coord.y, (chunkSize)),
            Util.Int_floor_division (coord.z, (chunkSize))
        );
        var voxelCoordInChunk = new Vector3Int (
            coord.x % (chunkSize),
            coord.y % (chunkSize),
            coord.z % (chunkSize)
        );

        if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize;
        if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize;
        if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize;

        // if (!chunkDictionary.ContainsKey (chunkCoord)) return new Voxel { density = 0 };

        return chunkDictionary[chunkCoord][voxelCoordInChunk];
    }
    private void SetVoxel (Vector3Int coord, Voxel voxel) {
        var chunkCoord = new Vector3Int (
            Util.Int_floor_division (coord.x, (chunkSize)),
            Util.Int_floor_division (coord.y, (chunkSize)),
            Util.Int_floor_division (coord.z, (chunkSize))
        );
        var voxelCoordInChunk = new Vector3Int (
            coord.x % (chunkSize),
            coord.y % (chunkSize),
            coord.z % (chunkSize)
        );

        if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize;
        if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize;
        if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize;

        if (!chunkDictionary.ContainsKey (chunkCoord)) throw new IndexOutOfRangeException ();
        chunkDictionary[chunkCoord][voxelCoordInChunk] = voxel;
    }
}