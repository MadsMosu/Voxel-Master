using System;
using UnityEngine;

[Serializable]
public class BaseHeightmapGenerator : FeatureGenerator {

    FastNoise noise = new FastNoise ();
    public override void Generate (WorldGeneratorSettings settings, VoxelChunk chunk) {

        noise.SetSeed (settings.seed);

        chunk.voxels.Traverse ((x, y, z, voxel) => {

            float density = noise.GetCubicFractal (
                ((chunk.coords.x * settings.voxelScale) * (chunk.size - 1f) + x),
                ((chunk.coords.y * settings.voxelScale) * (chunk.size - 1f) + y),
                ((chunk.coords.z * settings.voxelScale) * (chunk.size - 1f) + z));

            chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = density, materialIndex = 0 });
        });
    }
}