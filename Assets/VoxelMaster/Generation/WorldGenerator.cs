using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldGenerator
{

    WorldSettings worldSettings;

    public WorldGenerator(WorldSettings worldSettings)
    {
        this.worldSettings = worldSettings;
    }

    public void RequestChunkData(Action<ChunkData> callback)
    {
        ThreadStart threadStart = delegate
        {
            ChunkGenerationThread(callback);
        };
    }

    void ChunkGenerationThread(Action<ChunkData> callback)
    {
        var chunkData = GenerateChunkData();
    }

    ChunkData GenerateChunkData()
    {
        int size = 0;
        var voxels = new Voxel[size,size,size];
        // for (int x = 0; x < size + 1; x++)
        //     for (int y = 0; y < size + 1; y++)
        //         for (int z = 0; z < size + 1; z++)
        //         {
        //             voxels[x, y, z].Density = Perlin.Noise(((chunkCoordinates.x * size) + x) * 0.005f, ((chunkCoordinates.y * size) + y) * 0.005f, ((chunkCoordinates.z * size) + z) * 0.005f) * 2;
        //         }
        return new ChunkData() { voxels = voxels };
    }

}
