
using System;
using UnityEngine;

namespace VoxelMaster
{
    public abstract class ChunkDataStructure
    {
        public Chunk this[int x, int y, int z]
        {
            get { return GetChunk(new Vector3Int(x, y, z)); }
        }

        public abstract Chunk GetChunk(Vector3Int coords);

        public abstract bool ChunkExists(Vector3Int coords);

        public abstract void AddChunk(Vector3Int coords, Chunk chunk);

        public abstract void ForEach(Action<Chunk> func);
    }
}
