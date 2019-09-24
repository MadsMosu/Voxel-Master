using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MeshGenerator
{

    MeshSettings meshSettings;

    Queue<GenerationEvent> generatedChunkQueue = new Queue<GenerationEvent>();

    public MeshGenerator(MeshSettings worldSettings)
    {
        this.meshSettings = worldSettings;


    }

    public void RequestMeshData(Chunk chunk, Action<MeshData> callback)
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
            lock (generatedChunkQueue)
            {
                var @event = generatedChunkQueue.Dequeue();
                @event.callback.Invoke(@event.data);
            }
        }
    }

    void ChunkGenerationThread(Chunk chunk, Action<MeshData> callback)
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

    MeshData GenerateChunkData(Chunk chunk)
    {

        List<Triangle> triangles;

        MarchingCubes.GenerateMesh(chunk, out triangles, 0.2f);

        var verts = new List<Vector3>();
        var tris = new List<int>();

        int triIndex = 0;
        foreach (var triangle in triangles)
        {
            verts.Add(triangle.points[0]);
            tris.Add(triIndex + 2);
            verts.Add(triangle.points[1]);
            tris.Add(triIndex + 1);
            verts.Add(triangle.points[2]);
            tris.Add(triIndex);

            triIndex += 3;

        }


        return new MeshData()
        {
            coords = chunk.coords,
            vertices = verts.ToArray(),
            triangles = tris.ToArray()
        };
    }

    struct GenerationEvent
    {
        public Action<MeshData> callback;
        public MeshData data;
    }
}


public struct Triangle
{
    public Vector3[] points;

    public Triangle(Vector3[] points)
    {
        this.points = points;
    }
}