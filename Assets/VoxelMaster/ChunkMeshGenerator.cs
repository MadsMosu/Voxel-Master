
using UnityEngine;

public abstract class ChunkMeshGenerator
{
    public abstract MeshData GenerateMesh(Chunk chunk);
}


public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    // Vector2[] uvs;
}
