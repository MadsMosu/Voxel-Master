﻿using System;
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

        const float caveNoiseScale = .4f;
        const float caveRadius = 30f;


        float map(float s, float a1, float a2, float b1, float b2) {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

        public override void Generate(float[] heightmap, VoxelChunk chunk, WorldGeneratorSettings settings) {
            caveTunnelNoise.SetFractalType(FastNoise.FractalType.RigidMulti);
            caveTunnelNoise.SetFractalOctaves(1);
            caveTunnelNoise.SetSeed(settings.seed - 2333);

            caveHeightNoise.SetSeed(settings.seed + 98);
            caveHeightNoise.SetFrequency(.005f);
            caveHeightNoise.SetFractalOctaves(7);

            int sizeX = chunk.size.x;
            int chunkX = (int)(chunk.coords.x * settings.voxelScale) * chunk.size.x;
            int chunkY = (int)(chunk.coords.y * settings.voxelScale) * chunk.size.y;
            int chunkZ = (int)(chunk.coords.z * settings.voxelScale) * chunk.size.z;

            chunk.voxels.Traverse((x, y, z, voxel) => {
                float currentTerrainHeight = heightmap[Util.Map2DTo1D(x, z, sizeX)];
                float caveHeightModifier = caveHeightNoise.GetPerlinFractal((chunkX + x), (chunkZ + z)) * 100f;
                float caveCenterY = currentTerrainHeight - caveHeightModifier;

                float caveTunnel = caveTunnelNoise.GetCubicFractal((chunkX + x) / caveNoiseScale, (chunkZ + z) / caveNoiseScale);

                //var test = 0.8f - caveTunnel;
                var ampl = 1f;
                var tunnelRemap = map(caveTunnel, -1f, 1f, -(ampl * caveRadius * 0.33f), ampl);
                var caveDensity = tunnelRemap * (1f - Mathf.Min(1, Mathf.Abs(caveCenterY - (chunkY + y)) / (caveRadius * 0.33f)));

                voxel.density -= Mathf.Max(0, caveDensity * 50f);
                chunk.voxels.SetVoxel(x, y, z, voxel);
            });

        }
    }
}
