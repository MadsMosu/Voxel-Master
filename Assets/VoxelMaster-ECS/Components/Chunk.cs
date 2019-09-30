using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace VoxelMasterECS
{
    public struct Chunk : IComponentData
    {
        public NativeArray<Voxel> voxels;
        public Vector3Int coords;

        public enum ChunkStatus
        {
            CREATED, GENERATING, GENERATED_DATA, GENERATED_MESH
        }

        public ChunkStatus status;


        public Chunk(int size, Vector3Int coords)
        {
            voxels = new NativeArray<Voxel>(new Voxel[size * size * size], Allocator.Persistent);
            this.coords = coords;
            status = ChunkStatus.CREATED;
        }
    }
}
