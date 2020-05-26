using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.WorldGeneration;

namespace VoxelMaster.WorldGeneration.FeatureGenerators {
    class CaveGenerator : WorldFeatureGenerator {


        FastNoise caveHeightNoise = new FastNoise();
        FastNoise caveTunnelNoise = new FastNoise();

        const float caveNoiseScale = 1f;
        const float caveRadius = 5;

        public override void Generate(float[] heightmap, VoxelChunk chunk, WorldGeneratorSettings settings) {
            caveTunnelNoise.SetFractalType(FastNoise.FractalType.RigidMulti);
            caveTunnelNoise.SetFractalOctaves(1);
            caveTunnelNoise.SetSeed(settings.seed - 1);

            caveHeightNoise.SetSeed(settings.seed);

            int sizeX = chunk.size.x;
            int chunkX = (int)(chunk.coords.x * settings.voxelScale) * chunk.size.x;
            int chunkY = (int)(chunk.coords.y * settings.voxelScale) * chunk.size.y;
            int chunkZ = (int)(chunk.coords.z * settings.voxelScale) * chunk.size.z;

            chunk.voxels.Traverse((x, y, z, voxel) => {
                float currentTerrainHeight = heightmap[Util.Map2DTo1D(x, z, sizeX)];
                float caveHeightModifier = caveHeightNoise.GetPerlinFractal((chunkX + x) / caveNoiseScale, (chunkZ + z) / caveNoiseScale) * 6;
                float caveCenterY = currentTerrainHeight - caveHeightModifier;

                float caveTunnel = caveTunnelNoise.GetPerlinFractal((chunkX + x) / caveNoiseScale, (chunkZ + z) / caveNoiseScale);

                float caveDensityPreClamp = (Mathf.Abs(((chunkY + y)) - caveCenterY) / caveRadius) * caveTunnel;
                float caveDensity = 1 - Mathf.Min(1, (Mathf.Abs(((chunkY + y)) - caveCenterY) / caveRadius) * caveTunnel);

                var needSCope = 12;
            });

        }
    }
}
