using System.Collections.Generic;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;

public class FlattenTool : VoxelTool {
    public override string name => "Flatten Terrain";
    Plane plane;
    bool planeLocked = false;

    public override void OnToolGUI () { }

    public override void ToolDrag (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) {
        Vector3Int chunkWorldPosition = chunk.coords * (chunk.size);

        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
            chunk.dirty = true;
            //if within radius
            if (
                (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
                (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
            ) {
                float dist = plane.GetDistanceToPoint (voxelWorldPosition);
                float tempIntensity = intensity;
                if (falloff > 0) {
                    float scaleFactor = Vector3.Distance (voxelWorldPosition, position) * falloff;
                    tempIntensity /= scaleFactor;
                }
                v.density = Mathf.MoveTowards (v.density, dist, tempIntensity * Time.deltaTime);

                chunk.voxels.SetVoxel (voxelCoord.x, voxelCoord.y, voxelCoord.z, v);
            }
        });
    }

    public override void ToolEnd (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) {
        planeLocked = false;
    }

    public override void ToolStart (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) {
        if (planeLocked == false)
            plane = new Plane (surfaceNormal, position);
        planeLocked = true;
    }
}