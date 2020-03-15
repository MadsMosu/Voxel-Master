using System;
using UnityEngine;

[Serializable]
public class BaseHeightmapGenerator : FeatureGenerator {

    FastNoise noise = new FastNoise ();
    public override void Generate (WorldGeneratorSettings settings, VoxelChunk chunk) {

        noise.SetSeed (settings.seed);

        chunk.voxels.Traverse ((x, y, z, voxel) => {

            float height = settings.baseHeight + (noise.GetCubicFractal (
                ((chunk.coords.x * settings.voxelScale) * (chunk.size - 1) + x) / settings.noiseScale,
                ((chunk.coords.z * settings.voxelScale) * (chunk.size - 1) + z) / settings.noiseScale
            )) * settings.heightAmplifier;

            float density = ((((chunk.coords.y * settings.voxelScale) * (chunk.size - 1) + y) - height) / settings.heightAmplifier);

            sbyte voxelDensity = (sbyte) (density * 128.0000001f);
            chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = voxelDensity, materialIndex = 0 });
        });
    }
}