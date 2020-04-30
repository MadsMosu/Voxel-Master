using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace VoxelMaster.Chunk {
    public abstract class ChunkDataProvider {
        public abstract Task<VoxelChunk> RequestChunk(Vector3Int coord);
        public abstract Task<bool> HasChunk(Vector3Int coord);
    }
}