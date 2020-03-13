using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

class ThreadedMeshProvider {
    public WorldGeneratorSettings settings { get; private set; }
    VoxelMeshGenerator meshGenerator;

    private Queue<ChunkMeshGenerationData> generationQueue = new Queue<ChunkMeshGenerationData> ();
    private Queue<ChunkMeshGenerationData> generatedChunkQueue = new Queue<ChunkMeshGenerationData> ();
    public ThreadedMeshProvider (VoxelMeshGenerator meshGenerator, MeshGeneratorSettings meshGeneratorSettings) {
        this.settings = settings;
        this.meshGenerator = meshGenerator;
        meshGenerator.Init (meshGeneratorSettings);

        new Thread (new ThreadStart (delegate {
            while (true) {
                ProcessGenerationQueue ();
                Thread.Sleep (1);
            }
        })).Start ();
    }

    public void MainThreadUpdate () {
        if (generatedChunkQueue.Count > 0) {
            var data = generatedChunkQueue.Dequeue ();
            data.callback.Invoke (data);
        }
    }

    void ProcessGenerationQueue () {
        if (generationQueue.Count > 0) {
            var data = generationQueue.Dequeue ();
            StartGenerationThread (data.voxelChunk, data.callback);
        }
    }

    public void RequestChunkMesh (VoxelChunk chunk, Action<ChunkMeshGenerationData> onChunkData) {
        generationQueue.Enqueue (new ChunkMeshGenerationData {
            voxelChunk = chunk,
                callback = onChunkData,
        });
    }

    void StartGenerationThread (VoxelChunk chunk, Action<ChunkMeshGenerationData> onChunkData) {
        ThreadPool.QueueUserWorkItem (delegate {
            GenerateChunkDataThread (chunk, onChunkData);
        });
        // Task.Run (delegate {
        //     GenerateChunkDataThread (chunk, onChunkData);
        // });
    }

    void GenerateChunkDataThread (VoxelChunk chunk, Action<ChunkMeshGenerationData> onChunkData) {
        MeshData meshData = meshGenerator.GenerateMesh (chunk);
        chunk.SetMeshData (meshData);
        lock (generatedChunkQueue) {
            generatedChunkQueue.Enqueue (new ChunkMeshGenerationData {
                voxelChunk = chunk,
                    callback = onChunkData
            });
        }
    }
    public struct ChunkMeshGenerationData {
        public VoxelChunk voxelChunk;
        public Action<ChunkMeshGenerationData> callback;
    }

}