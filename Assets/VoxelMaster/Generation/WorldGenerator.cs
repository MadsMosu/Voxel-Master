﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldGenerator
{

    WorldSettings worldSettings;
    FastNoise fastNoise;

    Queue<GenerationEvent> generatedChunkQueue = new Queue<GenerationEvent>();

    public WorldGenerator(WorldSettings worldSettings)
    {
        this.worldSettings = worldSettings;
        fastNoise = new FastNoise();

    }

    public void RequestChunkData(Chunk chunk, Action<ChunkData> callback)
    {
        ThreadPool.QueueUserWorkItem(delegate
        {
            ChunkGenerationThread(chunk, callback);
        });
    }

    public void MainThreadUpdate()
    {
        if (generatedChunkQueue.Count > 0)
        {
            var @event = generatedChunkQueue.Dequeue();
            @event.callback.Invoke(@event.data);
        }
    }

    void ChunkGenerationThread(Chunk chunk, Action<ChunkData> callback)
    {
        var chunkData = GenerateChunkData(chunk);
        var generationEvent = new GenerationEvent()
        {
            callback = callback,
            data = chunkData
        };
        lock (generatedChunkQueue)
        {
            generatedChunkQueue.Enqueue(generationEvent);
        }

    }

    ChunkData GenerateChunkData(Chunk chunk)
    {
        var voxelGridSize = Mathf.CeilToInt((chunk.size + 1) * (1 / chunk.voxelSize));
        var voxels = new Voxel[voxelGridSize, voxelGridSize, voxelGridSize];
        for (int x = 0; x < voxelGridSize; x++)
            for (int y = 0; y < voxelGridSize; y++)
                for (int z = 0; z < voxelGridSize; z++)
                {
                    voxels[x, y, z].Density = fastNoise.GetPerlinFractal(
                        ((chunk.coords.x * chunk.size) + x * chunk.voxelSize) * 4f,
                        ((chunk.coords.y * chunk.size) + y * chunk.voxelSize) * 4f,
                        ((chunk.coords.z * chunk.size) + z * chunk.voxelSize) * 4f
                    ) + .25f;

                }
        return new ChunkData() { coords = chunk.coords, voxels = voxels };
    }



    struct GenerationEvent
    {
        public Action<ChunkData> callback;
        public ChunkData data;
    }
}
