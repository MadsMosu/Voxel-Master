using UnityEngine;
using VoxelMaster.Chunk;

namespace VoxelMaster.Core {
    public class OctreeNode<T> {
        public T item;
        public uint locationCode;
        public byte childrenFlags;
        public Bounds bounds;
    }
}