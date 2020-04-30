using Assets.VoxelMaster.Core;
using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VoxelMaster.Chunk;

namespace VoxelMaster.WorldGeneration {

    [Serializable]
    public class BaseHeightmapGenerator : FeatureGenerator {

        FastNoise noise = new FastNoise();

        public override void Generate(WorldGeneratorSettings settings, VoxelChunk chunk) {

            noise.SetSeed(settings.seed);

            var chunkSizeMinusOne = chunk.size - 1f;

            var chunkX = (chunk.coords.x * settings.voxelScale) * chunkSizeMinusOne;
            var chunkY = (chunk.coords.y * settings.voxelScale) * chunkSizeMinusOne;
            var chunkZ = (chunk.coords.z * settings.voxelScale) * chunkSizeMinusOne;

            float mountainScale = 2f;
            float mountainAmplifier = 70f;

            bool hasChecked = false;
            bool prevVoxelSign = false;
            chunk.voxels.Traverse((x, y, z, voxel) => {

                float mountainHeight = noise.GetPerlinFractal((chunkX + x) / mountainScale, 0, (chunkZ + z) / mountainScale);

                mountainHeight = ((1f - Math.Abs(mountainHeight)) * mountainAmplifier) - mountainAmplifier / 2;

                float density = (((chunk.coords.y * settings.voxelScale) * (chunk.size - 1f) + y) - mountainHeight);

                chunk.voxels.SetVoxel(new Vector3Int(x, y, z), new Voxel { density = density, materialIndex = 0 });

                if (hasChecked) {
                    if (prevVoxelSign != density < 0) chunk.hasSolids = true;
                }
                else {
                    hasChecked = true;
                }
                prevVoxelSign = density < 0;

            });
        }
    }
}
