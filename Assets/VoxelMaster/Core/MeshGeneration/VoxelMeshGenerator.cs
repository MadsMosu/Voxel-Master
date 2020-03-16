using System;
using UnityEngine;

public abstract class VoxelMeshGenerator {
    public abstract MeshData GenerateMesh (IVoxelData voxelData, Vector3Int origin, int size, int lod);

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
}