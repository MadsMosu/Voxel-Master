using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelChunk : IVoxelData {

    public VoxelWorld voxelWorld;

    public ChunkStatus status = ChunkStatus.Created;

    public VoxelDataStructure voxels { get; private set; }
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

    public Vector3Int coords { get; private set; }
    public int size { get; private set; }
    public float voxelScale { get; private set; }
    // public short[][][] reuseVertexCache;

    public Voxel this [Vector3 v] {
        get => this [new Vector3Int ((int) v.x, (int) v.y, (int) v.z)];
        set => this [new Vector3Int ((int) v.x, (int) v.y, (int) v.z)] = value;
    }
    public Voxel this [Vector3Int v] {
        get => GetVoxel (v);
        set => SetVoxel (v, value);
    }
    public Voxel this [int x, int y, int z] {
        get => this [new Vector3Int (x, y, z)];
        set => this [new Vector3Int (x, y, z)] = value;
    }

    public Mesh mesh;

    private MeshData meshData;

    public VoxelChunk (Vector3Int coords, int size, float voxelScale, VoxelDataStructure voxels) {
        this.coords = coords;
        this.size = size;
        this.voxelScale = voxelScale;
        this.voxels = voxels;
        this.voxels.Init (this.size);
    }

    //DONT DELETE BELOW - MIGHT BE USED FOR LATER WHEN IMPLEMENTING CACHING
    // private void InitReuseVertexCache () {
    //     reuseVertexCache = new short[2][][];
    //     reuseVertexCache[0] = new short[size * size][];
    //     reuseVertexCache[1] = new short[size * size][];

    //     for (int i = 0; i < (size * size); i++) {
    //         reuseVertexCache[0][i] = new short[4];
    //         reuseVertexCache[1][i] = new short[4];
    //         for (int j = 0; j < 4; j++) {
    //             reuseVertexCache[0][i][j] = -1;
    //             reuseVertexCache[1][i][j] = -1;
    //         }
    //     }
    // }

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

    private Voxel GetVoxel (Vector3Int coord) => voxels.GetVoxel (coord);
    private void SetVoxel (Vector3Int coord, Voxel voxel) => voxels.SetVoxel (coord, voxel);

    public void GenerateMesh () {
        mesh = new Mesh ();
        mesh.SetVertices (meshData.vertices);
        mesh.SetTriangles (meshData.triangleIndicies, 0);
        if (meshData.normals == null || meshData.normals.Length == 0) {
            mesh.RecalculateNormals ();
        } else {
            mesh.SetNormals (meshData.normals);
            mesh.RecalculateNormals ();
        }
        status = ChunkStatus.Idle;
    }

}

public enum ChunkStatus {
    Created,
    Loading,
    Idle
}