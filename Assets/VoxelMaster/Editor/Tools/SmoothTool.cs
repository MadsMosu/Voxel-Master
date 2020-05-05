using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;

public class SmoothTool : VoxelTool {
    public override string name => "Smooth Terrain";

    public override void OnToolGUI () {
        throw new System.NotImplementedException ();
    }
    public override void ToolStart (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        // SmoothTerrain (voxels, position, surfaceNormal);
    }

    public override void ToolDrag (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        // SmoothTerrain (voxels, position, surfaceNormal);
    }

    public override void ToolEnd (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        // Do nothing
    }
    private int radius = 4;
    private void SmoothTerrain (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal) {
        for (int z = -radius; z < radius; z++)
            for (int y = -radius; y < radius; y++)
                for (int x = -radius; x < radius; x++) {
                    //voxels[x, y, z] = GetAvgDensity(voxels, position);
                }
    }

    // private float GetAvgDensity (List<VoxelChunk> chunks, Vector3 position) {
    //     var sumDensity = voxels[position].density;
    //     int i = 0;
    //     for (int z = -1; z < 1; z++)
    //         for (int y = -1; y < 1; y++)
    //             for (int x = -1; x < 1; x++) {
    //                 sumDensity += voxels[(int) position.x + x, (int) position.y + y, (int) position.z + z].density;
    //                 i++;
    //             }

    //     return sumDensity / i;
    // }
}