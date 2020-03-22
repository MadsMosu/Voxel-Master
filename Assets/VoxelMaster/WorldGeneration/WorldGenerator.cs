using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class WorldGenerator {

    public WorldGeneratorSettings settings { get; private set; }

    private List<Type> featureGenerators = Util.GetEnumerableOfType<FeatureGenerator> ().ToList ();

    private Queue<ChunkGenerationData> generationQueue = new Queue<ChunkGenerationData> ();
    private Queue<ChunkGenerationData> generatedChunkQueue = new Queue<ChunkGenerationData> ();
    public WorldGenerator (WorldGeneratorSettings settings) {
        this.settings = settings;

        new Thread (new ThreadStart (delegate {
            while (true) {
                ProcessGenerationQueue ();
                Thread.Sleep (2);
            }
        })).Start ();
    }

    public void MainThreadUpdate () {

        if (generatedChunkQueue.Count > 0) {
            var data = generatedChunkQueue.Dequeue ();
            data.callback.Invoke (data.voxelChunk);
        }
    }

    void ProcessGenerationQueue () {
        if (generationQueue.Count > 0) {
            var data = generationQueue.Dequeue ();
            StartGenerationThread (data.voxelChunk, data.callback);
        }
    }

    public void RequestChunkData (VoxelChunk chunk, Action<VoxelChunk> onChunkData) {
        generationQueue.Enqueue (new ChunkGenerationData {
            voxelChunk = chunk,
                callback = onChunkData,
        });
    }

    void StartGenerationThread (VoxelChunk chunk, Action<VoxelChunk> onChunkData) {
        // ThreadPool.QueueUserWorkItem (delegate {
        //     GenerateChunkDataThread (chunk, onChunkData);
        // });
        Task.Run (delegate {
            GenerateChunkDataThread (chunk, onChunkData);
        });
    }

    FeatureGenerator featureGenerator = new BaseHeightmapGenerator ();
    void GenerateChunkDataThread (VoxelChunk chunk, Action<VoxelChunk> onChunkData) {
        featureGenerator.Generate (settings, chunk);
        lock (generatedChunkQueue) {
            generatedChunkQueue.Enqueue (new ChunkGenerationData {
                voxelChunk = chunk,
                    callback = onChunkData
            });
        }
    }
    public struct ChunkGenerationData {
        public VoxelChunk voxelChunk;
        public Action<VoxelChunk> callback;
    }

}