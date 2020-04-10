using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ThreadedMeshProvider {

    public WorldGeneratorSettings settings { get; private set; }
    VoxelMeshGenerator meshGenerator;

    private Queue<MeshGenerationRequest> generationQueue = new Queue<MeshGenerationRequest> ();
    private Queue<MeshGenerationResult> generatedChunkQueue = new Queue<MeshGenerationResult> ();
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
            StartGenerationThread (data);
        }
    }

    public void RequestChunkMesh (MeshGenerationRequest request) {
        generationQueue.Enqueue (request);
    }

    void StartGenerationThread (MeshGenerationRequest request) {
        Task.Run (delegate {
            GenerateChunkDataThread (request);
        });
    }

    void GenerateChunkDataThread (MeshGenerationRequest request) {
        MeshData meshData = meshGenerator.GenerateMesh (request.voxels, request.size, request.step, request.voxelScale);
        lock (generatedChunkQueue) {
            generatedChunkQueue.Enqueue (new MeshGenerationResult {
                locationCode = request.locationCode,
                    meshData = meshData,
                    callback = request.callback
            });
        }
    }

    public struct MeshGenerationRequest {
        public uint locationCode;
        public Voxel[] voxels;
        public float voxelScale;
        public int step;
        public int size;
        public Action<MeshGenerationResult> callback;
    }

    public struct MeshGenerationResult {
        public uint locationCode;
        public MeshData meshData;
        public Action<MeshGenerationResult> callback;
    }

}