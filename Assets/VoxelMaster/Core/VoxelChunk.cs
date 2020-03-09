using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelChunk {

    public ChunkStatus status { get; private set; } = ChunkStatus.Created;

    public VoxelDataStructure voxels { get; private set; }
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

    public Vector3Int size { get; private set; }
    public Vector3Int neighborFieldSize { get; private set; }
    public float voxelScale { get; private set; }
    public float isoLevel { get; private set; }

    private MeshData meshData;
    private VoxelChunk[] neighbors;

    public VoxelChunk (Vector3Int size, float voxelScale, float isoLevel, VoxelDataStructure voxels) {
        this.size = size;
        this.voxelScale = voxelScale;
        this.isoLevel = isoLevel;
        this.voxels = voxels;
        this.neighbors = new VoxelChunk[26];
        this.neighborFieldSize = size + new Vector3Int (size.x, size.y, size.z);
    }

    private int GetNeighborIndex (Vector3Int neighborPos) {
        neighborPos += Vector3Int.one;
        var index = Util.Map3DTo1D (neighborPos, new Vector3Int (3, 3, 3));
        if (index >= 13) {
            index -= 1;
        }
        return index;
    }

    public void AddNeighbor (VoxelChunk chunk, Vector3Int neighborPos) {
        neighbors[GetNeighborIndex (neighborPos)] = chunk;
    }

    public Voxel GetVoxel (Vector3Int coords) {
        if (
            coords.x >= 0 && coords.y >= 0 && coords.z >= 0 &&
            coords.x < size.x && coords.y < size.y && coords.z < size.z
        ) {
            return voxels.GetVoxel (coords);
        } else {
            Vector3Int relativeChunkPos = new Vector3Int (
                Int_floor_division (coords.x, size.x),
                Int_floor_division (coords.y, size.y),
                Int_floor_division (coords.z, size.z)
            );

            Vector3Int neighborPos = new Vector3Int (
                Math.Sign (relativeChunkPos.x),
                Math.Sign (relativeChunkPos.y),
                Math.Sign (relativeChunkPos.z)
            );
            VoxelChunk neighbor = neighbors[GetNeighborIndex (neighborPos)];

            if (neighbor != null) {
                Vector3Int voxelPos = new Vector3Int (
                    coords.x - size.x * neighborPos.x,
                    coords.y - size.y * neighborPos.y,
                    coords.z - size.z * neighborPos.z
                );
                return neighbor.GetVoxel (voxelPos);
            } else return default (Voxel);
        }
    }

    private int Int_floor_division (int lhs, int rhs) {
        int q = lhs / rhs;
        if (lhs % rhs < 0) {
            return q - 1;
        } else return q;
    }

    public void TraverseWithNeighbors (Action<int, int, int, Voxel> function) {
        int totalFieldSize = (size.x * size.y * size.z) + (size.x + size.y + size.z);
        for (int i = 0; i < totalFieldSize; i++) {
            var coord = Util.Map1DTo3D (i, neighborFieldSize);
            function.Invoke (coord.x, coord.y, coord.z, GetVoxel (coord));
        }
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

    public void GenerateMeshData (VoxelMeshGenerator generator, Func<Vector3, float> densityFunction) {
        status = ChunkStatus.Loading;
        meshData = generator.GenerateMesh (this, densityFunction);
        status = ChunkStatus.HasMeshData;
    }

    public Mesh BuildMesh () {
        var mesh = new Mesh ();
        mesh.SetVertices (meshData.vertices);
        mesh.SetTriangles (meshData.triangleIndicies, 0);
        if (meshData.normals == null) {
            mesh.RecalculateNormals ();
        } else {
            mesh.SetNormals (meshData.normals);
        }
        status = ChunkStatus.Idle;
        return mesh;
    }

}

public enum ChunkStatus {
    Created,
    Loading,
    HasMeshData,
    Idle
}