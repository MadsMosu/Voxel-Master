using UnityEngine;
using VoxelMaster.Chunk;

namespace VoxelMaster.Core {
    public class OctreeNode {
        public VoxelChunk chunk;
        public uint locationCode;
        public byte childrenFlags;
        public Bounds bounds;
    }
}