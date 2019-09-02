using UnityEngine;

public struct MeshData
{
    public Vector3Int coords;
    public Vector3[] vertices;
    public int[] triangles;

    public Mesh CreateMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}