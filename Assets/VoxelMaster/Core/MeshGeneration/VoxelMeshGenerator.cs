using UnityEngine;

public abstract class VoxelMeshGenerator
{
    public float isoLevel = .4f;
    public abstract MeshData generateMesh(VoxelChunk chunk);

}


public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public MeshData(Vector3[] vertices, int[] triangles)
    {
        this.vertices = vertices;
        this.triangles = triangles;
    }
}