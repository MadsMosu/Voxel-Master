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
    public Color[] vertexColors;
    public MeshData (Vector3[] vertices, int[] triangleIndicies) {
        this.vertices = vertices;
        this.triangleIndicies = triangleIndicies;
        this.normals = null;
        this.vertexColors = null;
    }

    public MeshData (Vector3[] vertices, int[] triangleIndicies, Vector3[] normals) {
        this.vertices = vertices;
        this.triangleIndicies = triangleIndicies;
        this.normals = normals;
        this.vertexColors = null;
    }

    public MeshData (Vector3[] vertices, int[] triangleIndicies, Vector3[] normals, Color[] vertexColors) {
        this.vertices = vertices;
        this.triangleIndicies = triangleIndicies;
        this.normals = normals;
        this.vertexColors = vertexColors;
    }

    public Mesh BuildMesh () {
        var mesh = new Mesh ();
        mesh.SetVertices (this.vertices);
        mesh.SetTriangles (this.triangleIndicies, 0);
        if (this.vertexColors != null) {
            mesh.SetColors (this.vertexColors);
        }
        if (this.normals == null || this.normals.Length == 0) {
            mesh.RecalculateNormals ();
        } else {
            mesh.SetNormals (this.normals);
        }
        return mesh;
    }
}