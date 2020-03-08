using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelChunk {

    public ChunkStatus status = ChunkStatus.Created;

    public VoxelDataStructure voxels { get; private set; }
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

    public Vector3Int coords { get; private set; }
    public Vector3Int size { get; private set; }
    public float voxelScale { get; private set; }
    public float isoLevel { get; private set; }

    public Mesh mesh;

    private MeshData meshData;

    public VoxelChunk (Vector3Int coords, Vector3Int size, float voxelScale, float isoLevel, VoxelDataStructure voxels) {
        this.coords = coords;
        this.size = size + Vector3Int.one;
        this.voxelScale = voxelScale;
        this.isoLevel = isoLevel;
        this.voxels = voxels;
        this.voxels.Init (this.size);
    }

    public void AddDensity (Vector3 pos, float[][][] densities) {
        throw new NotImplementedException ();
    }

    public void SetDensity (Vector3 pos, float[][][] densities) {
        throw new NotImplementedException ();
    }

    public void RemoveDensity (Vector3 pos, float[][][] densities) {
        throw new NotImplementedException ();
    }

    public VoxelMaterial GetMaterial (Vector3 pos) {
        throw new NotImplementedException ();
    }

    public void SetMaterial (Vector3 pos, byte materialIndex) {
        throw new NotImplementedException ();
    }

    public void SetMaterialInRadius (Vector3 pos, float radius, byte materialIndex) {
        throw new NotImplementedException ();
    }

    public void SetMeshData (MeshData meshData) {
        this.meshData = meshData;
    }

    public void GenerateMesh () {
        mesh = new Mesh ();
        mesh.SetVertices (meshData.vertices);
        mesh.SetTriangles (meshData.triangleIndicies, 0);
        mesh.RecalculateNormals ();
        status = ChunkStatus.Idle;
    }

}

public enum ChunkStatus {
    Created,
    Loading,
    Idle
}