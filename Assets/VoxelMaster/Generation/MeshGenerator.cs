using System;
using System.Threading;
using UnityEngine;

public abstract class MeshGenerator
{

    public abstract MeshData GenerateMesh();


    public void RequestMesh(Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshGenerationThread(callback);
        };
    }

    private void MeshGenerationThread(Action<MeshData> callback)
    {
        var meshData = GenerateMesh();

    }

}

public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
}