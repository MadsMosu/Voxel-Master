using System;
using UnityEngine;

[Serializable]
public class BaseHeightmapGenerator : FeatureGenerator {

    FastNoise noise = new FastNoise ();
    public override void Generate (WorldGeneratorSettings settings, VoxelChunk chunk) {

        noise.SetSeed (settings.seed);

        chunk.voxels.Traverse ((x, y, z, voxel) => {

            float height = settings.baseHeight + Mathf.Pow ((noise.GetCubicFractal (
                ((chunk.coords.x * settings.voxelScale) * (chunk.size - 1) + x) / settings.noiseScale,
                ((chunk.coords.z * settings.voxelScale) * (chunk.size - 1) + z) / settings.noiseScale
            ) + .5f) * settings.heightAmplifier, 1.6f);

            float voxelDensity = (((chunk.coords.y * settings.voxelScale) * (chunk.size - 1) + y) - height) / settings.heightAmplifier;
            chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = (sbyte) (voxelDensity * 128.0), materialIndex = 0 });
        });
    }
}