
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System;

namespace VoxelMaster.Chunk.ChunkProviders {
    public class FileChunkDataProvider : ChunkDataProvider {
        string folderPath;
        ChunkSerializer chunkSerializer;

        public FileChunkDataProvider(string folderPath) {
            this.folderPath = folderPath;
            this.chunkSerializer = new ChunkSerializer(folderPath);
        }

        public override void RequestChunk(Vector3Int coord, Action<VoxelChunk> onChunk) {
            onChunk(chunkSerializer.LoadChunk(coord));
        }

        public override async Task<bool> HasChunk(Vector3Int coord) {
            return chunkSerializer.GetAvailableChunks().Contains(coord);
        }


    }
}
