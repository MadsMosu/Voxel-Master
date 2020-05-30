using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.WorldGeneration.FeatureGenerators;

namespace VoxelMaster.WorldGeneration {
    public class WorldGenerator {

        public WorldGeneratorSettings settings { get; private set; }

        private List<Type> featureGenerators = Util.GetEnumerableOfType<WorldFeatureGenerator> ().ToList ();

        private Queue<ChunkGenerationData> generationQueue = new Queue<ChunkGenerationData> ();
        private Queue<ChunkGenerationData> generatedChunkQueue = new Queue<ChunkGenerationData> ();
        public WorldGenerator (WorldGeneratorSettings settings) {
            this.settings = settings;
            this.heightmapGenerator = new WorldHeightmapGenerator (settings);

            new Thread (new ThreadStart (delegate {
                while (true) {
                    ProcessGenerationQueue ();
                    // Thread.Sleep (1);
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
            ThreadPool.QueueUserWorkItem (delegate {
                GenerateChunkDataThread (chunk, onChunkData);
            });
            Task.Run (delegate {
                GenerateChunkDataThread (chunk, onChunkData);
            });
        }

        WorldHeightmapGenerator heightmapGenerator;
        CaveGenerator caveGenerator = new CaveGenerator ();
        void GenerateChunkDataThread (VoxelChunk chunk, Action<VoxelChunk> onChunkData) {

            float[] heightmap = new float[chunk.size.x * chunk.size.z];

            heightmapGenerator.Generate (settings, chunk, out heightmap);
            caveGenerator.Generate (heightmap, chunk, settings);
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
}