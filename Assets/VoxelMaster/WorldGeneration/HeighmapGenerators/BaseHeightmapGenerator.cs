using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VoxelMaster.Chunk;

namespace VoxelMaster.WorldGeneration {

    [Serializable]
    public class BaseHeightmapGenerator : FeatureGenerator {

        FastNoise baseHeightNoise = new FastNoise ();
        FastNoise detailHeightNoise = new FastNoise ();
        FastNoise rigedNoise = new FastNoise ();

        float map (float s, float a1, float a2, float b1, float b2) {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

        public override void Generate (WorldGeneratorSettings settings, VoxelChunk chunk) {

            baseHeightNoise.SetSeed (settings.seed);

            detailHeightNoise.SetSeed (settings.seed);
            detailHeightNoise.SetFractalOctaves (3);

            rigedNoise.SetSeed (settings.seed);
            rigedNoise.SetFractalOctaves (8);
            rigedNoise.SetFractalType (FastNoise.FractalType.RigidMulti);

            var chunkSizeMinusOne = chunk.size.x - 1f;

            var chunkX = (chunk.coords.x * settings.voxelScale) * chunkSizeMinusOne;
            var chunkY = (chunk.coords.y * settings.voxelScale) * chunkSizeMinusOne;
            var chunkZ = (chunk.coords.z * settings.voxelScale) * chunkSizeMinusOne;

            float baseHeightScale = 5f;
            float baseHeightAmplifier = 200f;

            float caveScale = 2f;
            float caveThreshold = .0001f;

            bool hasChecked = false;
            bool prevVoxelSign = false;
            baseHeightNoise.SetFractalType (FastNoise.FractalType.FBM);
            chunk.voxels.Traverse ((x, y, z, voxel) => {

                float baseHeight = baseHeightNoise.GetPerlinFractal ((chunkX + x) / baseHeightScale, 0, (chunkZ + z) / baseHeightScale) * baseHeightAmplifier;

                baseHeight += detailHeightNoise.GetPerlinFractal ((chunkX + x) / (baseHeightScale / 20), 0, (chunkZ + z) / (baseHeightScale / 20)) * (baseHeightAmplifier / 50);

                float density = -(((chunk.coords.y * settings.voxelScale) * (chunk.size.y - 1f) + y) - baseHeight);

                var erosion = baseHeightNoise.GetSimplexFractal ((chunkX + x) / caveScale, (chunkY + y) / caveScale, (chunkZ + z) / caveScale);
                density -= Mathf.Clamp (erosion * 100, 0, Mathf.Infinity);

                chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel (density));

                if (hasChecked) {
                    if (prevVoxelSign != density < 0) chunk.hasSolids = true;
                } else hasChecked = true;

                prevVoxelSign = density < 0;

            });
        }
    }
}