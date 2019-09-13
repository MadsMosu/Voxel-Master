using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldGenerator
{

    WorldSettings worldSettings;
    TerrainGraph terrainGraph;


    Queue<GenerationEvent> generatedChunkQueue = new Queue<GenerationEvent>();

    public WorldGenerator(WorldSettings worldSettings, TerrainGraph terrainGraph)
    {
        this.worldSettings = worldSettings;
        this.terrainGraph = terrainGraph;

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
        var voxelGridSize = Mathf.CeilToInt((chunk.size) * (1 / chunk.voxelSize));
        var voxels = new Voxel[voxelGridSize * voxelGridSize * voxelGridSize];
        for (int x = 0; x < voxelGridSize; x++)
            for (int y = 0; y < voxelGridSize; y++)
                for (int z = 0; z < voxelGridSize; z++)
                {
                    var pos = new Vector3(
                        ((chunk.coords.x * (chunk.size - 1)) + x * chunk.voxelSize) * 4f,
                        ((chunk.coords.y * (chunk.size - 1)) + y * chunk.voxelSize) * 4f,
                        ((chunk.coords.z * (chunk.size - 1)) + z * chunk.voxelSize) * 4f
                    );

                    voxels[chunk.MapIndexTo1D(x, y, z)].Density = terrainGraph.Evaluate(pos);

                }
        return new ChunkData() { coords = chunk.coords, voxels = voxels };
    }



    struct GenerationEvent
    {
        public Action<ChunkData> callback;
        public ChunkData data;
    }
}
