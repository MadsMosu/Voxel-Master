using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VoxelMaster.WorldGeneration;
using VoxelMaster.WorldGeneration.FeatureGenerators;

namespace VoxelMaster.Chunk.ChunkProviders {
    public class GeneratorChunkDataProvider : ChunkDataProvider {
        WorldGeneratorSettings settings;

        public GeneratorChunkDataProvider (WorldGeneratorSettings settings) {
            this.settings = settings;
            this.generator = new WorldHeightmapGenerator (settings);
        }

        WorldHeightmapGenerator generator;
        CaveGenerator caveGenerator = new CaveGenerator ();
        public override void RequestChunk (Vector3Int coord, Action<VoxelChunk> onChunk) {
            Task.Run (() => {
                var chunk = new VoxelChunk (coord, new Vector3Int (16, 16, 16), 1, new JaggedDataStructure ());
                float[] heightmap = new float[chunk.size.x * chunk.size.z];
                generator.Generate (settings, chunk, out heightmap);
                //caveGenerator.Generate(heightmap, chunk, settings);
                onChunk (chunk);
            });

        }

        public override async Task<bool> HasChunk (Vector3Int coord) {
            return true; // Returning true since we are always able to provide a chunk
        }

    }

}