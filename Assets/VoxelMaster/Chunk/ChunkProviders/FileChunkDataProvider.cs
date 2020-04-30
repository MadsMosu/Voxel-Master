
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace VoxelMaster.Chunk.ChunkProviders {
    public class FileChunkDataProvider : ChunkDataProvider {
        string folderPath;
        ChunkSerializer chunkSerializer;

        public FileChunkDataProvider(string folderPath) {
            this.folderPath = folderPath;
            this.chunkSerializer = new ChunkSerializer(folderPath);
        }

        public override async Task<VoxelChunk> RequestChunk(Vector3Int coord) {
            return chunkSerializer.LoadChunk(coord);
        }

        public override async Task<bool> HasChunk(Vector3Int coord) {
            return chunkSerializer.GetAvailableChunks().Contains(coord);
        }
    }
}
