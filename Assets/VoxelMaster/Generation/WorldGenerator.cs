using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldGenerator
{

    WorldSettings worldSettings;

    Queue<GenerationEvent> generatedChunkQueue = new Queue<GenerationEvent>();

    public WorldGenerator(WorldSettings worldSettings)
    {
        this.worldSettings = worldSettings;


    }

    public void RequestChunkData(Chunk chunk, Action<ChunkData> callback)
    {

        ThreadStart threadStart = delegate
        {
            ChunkGenerationThread(chunk, callback);
        };

        new Thread(threadStart).Start();
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
        generatedChunkQueue.Enqueue(generationEvent);

    }

    ChunkData GenerateChunkData(Chunk chunk)
    {
        var voxelGridSize = chunk.size + 1;
        var voxels = new Voxel[voxelGridSize, voxelGridSize, voxelGridSize];
        for (int x = 0; x < voxelGridSize; x++)
            for (int y = 0; y < voxelGridSize; y++)
                for (int z = 0; z < voxelGridSize; z++)
                {
                    voxels[x, y, z].Density = Perlin.Noise(
                        ((chunk.coords.x * chunk.size) + x) * 0.05f,
                        ((chunk.coords.y * chunk.size) + y) * 0.05f,
                        ((chunk.coords.z * chunk.size) + z) * 0.05f
                        );


                }
        return new ChunkData() { coords = chunk.coords, voxels = voxels };
    }

    struct GenerationEvent
    {
        public Action<ChunkData> callback;
        public ChunkData data;
    }
}
