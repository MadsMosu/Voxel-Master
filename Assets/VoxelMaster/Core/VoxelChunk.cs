using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelChunk : IVoxelData {

    public VoxelWorld voxelWorld;

    private ThreadedMeshProvider meshProvider;
    public ChunkStatus status = ChunkStatus.Idle;

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

    public struct LODMesh {
        public int lod;
        public Mesh mesh;
    }
    public LODMesh[] LODMeshes;
    private int _lod = 0;

    public int lod {
        get => _lod;
        set => SetLod (value);
    }

    public void SetLod (int lod) {
        if (LODMeshes[lod].mesh == null) {
            this._lod = lod;
            meshProvider.RequestChunkMesh (this, OnMeshGenerated);
        }
    }

    private void OnMeshGenerated (ThreadedMeshProvider.ChunkMeshDataResult obj) {
        LODMeshes[obj.lod] = new LODMesh { lod = obj.lod, mesh = BuildMesh (obj.meshData) };
    }

    public Mesh GetCurrentMesh () {
        return LODMeshes[lod].mesh;
    }

    public VoxelChunk (Vector3Int coords, int size, float voxelScale, VoxelDataStructure voxels, ThreadedMeshProvider meshProvider) {
        this.coords = coords;
        this.size = size;
        this.voxelScale = voxelScale;
        this.voxels = voxels;
        this.voxels.Init (this.size);
        this.meshProvider = meshProvider;
        LODMeshes = new LODMesh[] { new LODMesh { lod = 0 }, new LODMesh { lod = 1 }, new LODMesh { lod = 2 }, new LODMesh { lod = 3 } };
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

    private Voxel GetVoxel (Vector3Int coord) => voxels.GetVoxel (coord);
    private void SetVoxel (Vector3Int coord, Voxel voxel) => voxels.SetVoxel (coord, voxel);

    public Mesh BuildMesh (MeshData meshData) {
        Mesh mesh = new Mesh ();
        mesh.SetVertices (meshData.vertices);
        mesh.SetTriangles (meshData.triangleIndicies, 0);
        if (meshData.normals == null || meshData.normals.Length == 0) {
            mesh.RecalculateNormals ();
        } else {
            mesh.SetNormals (meshData.normals);
        }
        return mesh;
    }

}

public enum ChunkStatus {
    HasData,
    GeneratingMesh,
    HasMeshData,
    Idle
}