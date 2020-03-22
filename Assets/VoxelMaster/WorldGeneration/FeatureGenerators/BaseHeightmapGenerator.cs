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

        chunk.voxels.Traverse ((x, y, z, voxel) => {

            float density = noise.GetPerlin (
                (chunkX + x) * 2f,
                (chunkY + y) * 2f,
                (chunkZ + z) * 2f);

            chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = density, materialIndex = 0 });
        });
    }
}