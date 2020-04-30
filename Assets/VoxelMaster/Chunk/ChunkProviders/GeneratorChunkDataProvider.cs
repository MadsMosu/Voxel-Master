using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VoxelMaster.WorldGeneration;

namespace VoxelMaster.Chunk.ChunkProviders {
    public class GeneratorChunkDataProvider : ChunkDataProvider {
        WorldGeneratorSettings settings;

        public GeneratorChunkDataProvider(WorldGeneratorSettings settings) {
            this.settings = settings;
        }

        BaseHeightmapGenerator generator = new BaseHeightmapGenerator();
        public override async Task<VoxelChunk> RequestChunk(Vector3Int coord) {
            var chunk = new VoxelChunk(coord, 17, 1, new SimpleDataStructure());
            generator.Generate(settings, chunk);
            return chunk;
        }

        public override async Task<bool> HasChunk(Vector3Int coord) {
            return true; // Returning true since we are always able to provide a chunk
        }
    }


}