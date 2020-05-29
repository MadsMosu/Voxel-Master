using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VoxelMaster.Chunk;

namespace VoxelMaster.WorldGeneration {

    [Serializable]
    public class WorldHeightmapGenerator {

        FastNoise baseRiverNoise = new FastNoise();
        FastNoise baseFractalNoise = new FastNoise();



        public WorldHeightmapGenerator(WorldGeneratorSettings settings) {
            baseRiverNoise.SetSeed(settings.seed);
            baseRiverNoise.SetFractalOctaves(1);
            baseRiverNoise.SetFractalType(FastNoise.FractalType.RigidMulti);
            baseRiverNoise.SetFrequency(0.001f);

            baseFractalNoise.SetSeed(settings.seed);
            baseFractalNoise.SetFractalOctaves(4);
        }

        public void Generate(WorldGeneratorSettings settings, VoxelChunk chunk, out float[] heightmap) {

            var sizeX = chunk.size.x;
            var sizeY = chunk.size.y;
            var chunkX = (chunk.coords.x * settings.voxelScale) * chunk.size.x;
            var chunkY = (chunk.coords.y * settings.voxelScale) * chunk.size.y;
            var chunkZ = (chunk.coords.z * settings.voxelScale) * chunk.size.z;

            float baseHeightScale = 10f;
            float baseHeightAmplifier = 200f;

            bool hasChecked = false;
            bool prevVoxelSign = false;

            float[] heightMapOutput = new float[sizeX * sizeY];
            chunk.voxels.Traverse((x, y, z, voxel) => {

                float worldX = chunkX + x;
                float worldY = chunkY + y;
                float worldZ = chunkZ + z;

                var density = Mathf.Clamp(SampleDensity(worldX, worldY, worldZ), -1, 1);

                chunk.voxels.SetVoxel(x, y, z, new Voxel { density = density });

                if (hasChecked) {
                    if (prevVoxelSign != density < .5f) chunk.hasSolids = true;
                }
                else hasChecked = true;

                prevVoxelSign = density < .5f;

            });
            heightmap = heightMapOutput;
        }

        public float SampleDensity(float x, float y, float z) {

            float baseRiverHeight = 1f - baseRiverNoise.GetPerlinFractal(x, 0, z);
            float baseFractalHeight = baseFractalNoise.GetPerlinFractal(x, 0, z);

            float baseHeight = baseRiverHeight * 80f;
            baseHeight += baseRiverHeight * (baseFractalHeight * 150);

            return 1f - (y - baseHeight);
        }
    }
}