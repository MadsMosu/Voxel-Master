using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelMaster
{
    public struct Chunk
    {
        public Vector3Int Coords { get; private set; }
        public NativeArray<float> Voxels { get; private set; }
        public int Size { get; private set; }

        public enum ChunkStatus
        {
            CREATED, GENERATING, GENERATED_DATA, GENERATED_MESH
        }

        public ChunkStatus Status;

        public Chunk(Vector3Int coords, int size)
        {
            this.Coords = coords;
            this.Size = size;
            this.Status = ChunkStatus.CREATED;

            this.Voxels = new NativeArray<float>(Size * Size * Size, Allocator.Persistent);
        }

    }
}
