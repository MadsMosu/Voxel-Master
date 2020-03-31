using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ThreadedMeshProvider {
    public WorldGeneratorSettings settings { get; private set; }
    VoxelMeshGenerator meshGenerator;

    private Queue<ChunkMeshGenerationData> generationQueue = new Queue<ChunkMeshGenerationData> ();
    private Queue<ChunkMeshDataResult> generatedChunkQueue = new Queue<ChunkMeshDataResult> ();
    public ThreadedMeshProvider (VoxelMeshGenerator meshGenerator, MeshGeneratorSettings meshGeneratorSettings) {
        this.settings = settings;
        this.meshGenerator = meshGenerator;
        meshGenerator.Init (meshGeneratorSettings);

        new Thread (new ThreadStart (delegate {
            while (true) {
                ProcessGenerationQueue ();
                // Thread.Sleep (50);
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

    public void RequestChunkMesh (VoxelChunk chunk, Action<ChunkMeshDataResult> onChunkData) {
        generationQueue.Enqueue (new ChunkMeshGenerationData {
            voxelChunk = chunk,
                callback = onChunkData,
        });
    }

    void StartGenerationThread (VoxelChunk chunk, Action<ChunkMeshDataResult> onChunkData) {
        Task.Run (delegate {
            GenerateChunkDataThread (chunk, onChunkData);
        });
    }

    void GenerateChunkDataThread (VoxelChunk chunk, Action<ChunkMeshDataResult> onChunkData) {
        // MeshData meshData = meshGenerator.GenerateMesh (chunk.voxelWorld, chunk.coords * chunk.size, chunk.size, chunk.lod);
        // lock (generatedChunkQueue) {
        //     generatedChunkQueue.Enqueue (new ChunkMeshDataResult {
        //         meshData = meshData,
        //             lod = chunk.lod,
        //             callback = onChunkData
        //     });
        // }
    }
    public struct ChunkMeshGenerationData {
        public VoxelChunk voxelChunk;
        public Action<ChunkMeshDataResult> callback;
    }

    public struct ChunkMeshDataResult {
        public MeshData meshData;
        public int lod;
        public Action<ChunkMeshDataResult> callback;
    }

}