using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VoxelMaster.Core.MeshProviders {

    public class ThreadedMeshProvider {

        public WorldGeneratorSettings settings { get; private set; }
        VoxelMeshGenerator meshGenerator;
        private IVoxelData volume;

        private Queue<MeshGenerationRequest> generationQueue = new Queue<MeshGenerationRequest>();
        private Queue<MeshGenerationResult> generatedChunkQueue = new Queue<MeshGenerationResult>();

        private Thread thread;

        public ThreadedMeshProvider(IVoxelData volume, VoxelMeshGenerator meshGenerator, MeshGeneratorSettings meshGeneratorSettings) {
            this.settings = settings;
            this.meshGenerator = meshGenerator;
            this.volume = volume;
            meshGenerator.Init(meshGeneratorSettings);

            thread = new Thread(new ThreadStart(delegate {
                while (true) {
                    ProcessGenerationQueue();
                    // Thread.Sleep (50);
                }
            }));
            thread.Start();
        }

        public void MainThreadUpdate() {
            if (generatedChunkQueue.Count > 0) {
                var data = generatedChunkQueue.Dequeue();
                data.callback.Invoke(data);
            }
        }

        void ProcessGenerationQueue() {
            if (generationQueue.Count > 0) {
                var data = generationQueue.Dequeue();
                StartGenerationThread(data);
            }
        }

        public void RequestChunkMesh(MeshGenerationRequest request) {
            generationQueue.Enqueue(request);
        }

        void StartGenerationThread(MeshGenerationRequest request) {
            Task.Run(delegate {
                GenerateChunkDataThread(request);
            });
        }

        void GenerateChunkDataThread(MeshGenerationRequest request) {
            var s = new Stopwatch();
            s.Start();
            MeshData meshData = meshGenerator.GenerateMesh(volume, request.origin, request.step, request.voxelScale);
            s.Stop();
            //UnityEngine.Debug.Log(s.ElapsedMilliseconds);
            lock (generatedChunkQueue) {
                generatedChunkQueue.Enqueue(new MeshGenerationResult {
                    locationCode = request.locationCode,
                    meshData = meshData,
                    callback = request.callback
                });
            }
        }

    }
    public struct MeshGenerationRequest {
        public Vector3Int origin;
        public uint locationCode;
        public float voxelScale;
        public int step;
        public Action<MeshGenerationResult> callback;
    }

    public struct MeshGenerationResult {
        public uint locationCode;
        public MeshData meshData;
        public Action<MeshGenerationResult> callback;
    }
}