using System.Collections.Generic;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;

public class SmoothTool : VoxelTool {
    public override string name => "Smooth Terrain";

    public override void OnToolGUI () { }
    public override void ToolStart (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) {

    }

    public override void ToolDrag (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) {
        Vector3Int chunkWorldPosition = chunk.coords * (chunk.size);
        chunk.dirty = true;

        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
            if (
                (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
                (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
            ) {
                float tempIntensity = intensity;
                if (falloff > 0) {
                    float scaleFactor = Vector3.Distance (voxelWorldPosition, position) * falloff;
                    tempIntensity /= scaleFactor;
                }
                float avgDensity = getAvgDensity (chunk, voxelCoord, voxelWorld, chunkWorldPosition);
                if (Mathf.Abs (avgDensity - v.density) > 0.20f) {
                    v.density = Mathf.MoveTowards (v.density, avgDensity, (tempIntensity / 2 + 0.02f * Mathf.Abs (avgDensity - v.density)) * Time.deltaTime);
                    chunk.voxels.SetVoxel (voxelCoord, v);
                }
            }
        });

    }

    public override void ToolEnd (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) {
        // Do nothing
    }

    private float getAvgDensity (VoxelChunk chunk, Vector3Int voxelCoord, VoxelWorld voxelWorld, Vector3Int chunkWorldPosition) {
        int i = 0;
        float sumDensity = 0f;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++) {
                    if (x == 0 || y == 0 || z == 0) continue;
                    Vector3Int neighborCoord = new Vector3Int (x, y, z);
                    Voxel neighborVoxel;
                    if (
                        (voxelCoord.x + x < 0 || voxelCoord.x + x >= chunk.size.x) ||
                        (voxelCoord.y + y < 0 || voxelCoord.y + y >= chunk.size.y) ||
                        (voxelCoord.z + z < 0 || voxelCoord.z + z >= chunk.size.z)
                    ) neighborVoxel = voxelWorld[chunkWorldPosition + voxelCoord + neighborCoord];
                    else
                        neighborVoxel = chunk.voxels.GetVoxel (voxelCoord + neighborCoord);

                    sumDensity += neighborVoxel.density;
                    i++;
                }
        return sumDensity / i;
    }
}