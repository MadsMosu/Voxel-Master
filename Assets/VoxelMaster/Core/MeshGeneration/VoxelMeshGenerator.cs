using UnityEngine;

public abstract class VoxelMeshGenerator
{
    public float isoLevel = .4f;
    public abstract MeshData generateMesh(VoxelChunk chunk);

}


public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangleIndicies;
    public MeshData(Vector3[] vertices, int[] triangleIndicies)
    {
        this.vertices = vertices;
        this.triangleIndicies = triangleIndicies;
    }
}