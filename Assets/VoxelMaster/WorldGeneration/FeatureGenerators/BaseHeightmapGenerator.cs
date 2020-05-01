using VoxelMaster.Core;
using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VoxelMaster.Chunk;

namespace VoxelMaster.WorldGeneration {

    [Serializable]
    public class BaseHeightmapGenerator : FeatureGenerator {

        FastNoise noise = new FastNoise();

        float map(float s, float a1, float a2, float b1, float b2) {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

        public override void Generate(WorldGeneratorSettings settings, VoxelChunk chunk) {

            noise.SetSeed(settings.seed);

            var chunkSizeMinusOne = chunk.size - 1f;

            var chunkX = (chunk.coords.x * settings.voxelScale) * chunkSizeMinusOne;
            var chunkY = (chunk.coords.y * settings.voxelScale) * chunkSizeMinusOne;
            var chunkZ = (chunk.coords.z * settings.voxelScale) * chunkSizeMinusOne;

            float mountainScale = 2f;
            float mountainAmplifier = 70f;

            float caveScale = 2f;
            float caveThreshold = .0001f;

            bool hasChecked = false;
            bool prevVoxelSign = false;
            noise.SetFractalType(FastNoise.FractalType.RigidMulti);
            chunk.voxels.Traverse((x, y, z, voxel) => {

                float mountainHeight = noise.GetPerlinFractal((chunkX + x) / mountainScale, 0, (chunkZ + z) / mountainScale);

                //mountainHeight = (((1f - Math.Abs(mountainHeight)) * mountainAmplifier) - mountainAmplifier / 2);

                float density = -(((chunk.coords.y * settings.voxelScale) * (chunk.size - 1f) + y) - mountainHeight);



                //var caveNosie = noise.GetSimplexFractal((chunkX + x) / caveScale, (chunkY + y) / caveScale, (chunkZ + z) / caveScale);
                //density -= Mathf.Clamp(caveNosie * 100, 0, Mathf.Infinity);



                chunk.voxels.SetVoxel(new Vector3Int(x, y, z), new Voxel { density = density, materialIndex = 0 });

                if (hasChecked) {
                    if (prevVoxelSign != density < 0) chunk.hasSolids = true;
                }
                else hasChecked = true;

                prevVoxelSign = density < 0;

            });
        }
    }
}
