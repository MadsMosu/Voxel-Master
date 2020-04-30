using System;
using UnityEngine;

public abstract class VoxelMeshGenerator {
    public abstract MeshData GenerateMesh (IVoxelData volume, Vector3 origin, int step, float scale);

    public abstract void Init (MeshGeneratorSettings settings);

}

public struct MeshData {
    public Vector3[] vertices;
    public int[] triangleIndicies;
    public Vector3[] normals;
    public MeshData (Vector3[] vertices, int[] triangleIndicies) {
        this.vertices = vertices;
        this.triangleIndicies = triangleIndicies;
        normals = null;
    }

    public MeshData (Vector3[] vertices, int[] triangleIndicies, Vector3[] normals) {
        this.vertices = vertices;
        this.triangleIndicies = triangleIndicies;
        this.normals = normals;
    }

    public Mesh BuildMesh () {
        var mesh = new Mesh ();
        mesh.SetVertices (this.vertices);
        mesh.SetTriangles (this.triangleIndicies, 0);
        if (this.normals == null || this.normals.Length == 0) {
            mesh.RecalculateNormals ();
        } else {
            mesh.SetNormals (this.normals);
        }
        return mesh;
    }
}