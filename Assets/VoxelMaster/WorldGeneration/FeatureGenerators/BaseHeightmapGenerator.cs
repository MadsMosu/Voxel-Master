using System;
using UnityEngine;

[Serializable]
public class BaseHeightmapGenerator : FeatureGenerator {

    FastNoise noise = new FastNoise ();

    public override void Generate (WorldGeneratorSettings settings, VoxelChunk chunk) {

        noise.SetSeed (settings.seed);

        var chunkSizeMinusOne = chunk.size - 1f;
        var chunkX = (chunk.coords.x * settings.voxelScale) * chunkSizeMinusOne;
        var chunkY = (chunk.coords.y * settings.voxelScale) * chunkSizeMinusOne;
        var chunkZ = (chunk.coords.z * settings.voxelScale) * chunkSizeMinusOne;

        float noiseScale = 1f;
        chunk.voxels.Traverse ((x, y, z, voxel) => {

            // float terrainHeight = noise.GetPerlinFractal ((chunkX + x) / noiseScale, 0, (chunkZ + z) / noiseScale);
            float terrainHeight = noise.GetPerlin ((chunkX + x) / noiseScale, 0, (chunkZ + z) / noiseScale) + 2;
            // float terrainHeight = 3;

            // terrainHeight = 1f - Math.Abs (terrainHeight);

            float density = (((chunk.coords.y * settings.voxelScale) * (chunk.size - 1f) + y) - (terrainHeight * 5)) / settings.heightAmplifier;
            // density += noise.GetPerlin ((chunkX + x) / (noiseScale / 20), 0, (chunkZ + z) / (noiseScale / 20)) / 10f;

            chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = -density, materialIndex = 0 });

        });
    }
}