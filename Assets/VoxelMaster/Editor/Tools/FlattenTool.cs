using System.Collections.Generic;
using UnityEngine;
using VoxelMaster.Chunk;

public class FlattenTool : VoxelTool {
    public override string name => "Flatten Terrain";

    public override void OnToolGUI () {
        throw new System.NotImplementedException ();
    }

    public override void ToolDrag (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, int falloff) {
        Vector3Int chunkWorldPosition = chunk.coords * chunk.size;
        Plane plane = new Plane (surfaceNormal, Vector3.zero); //insert avg mesh vertex and avg mesh normal

        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
            //if within radius
            if (
                (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
                (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
            ) {
                float dist = plane.GetDistanceToPoint (voxelWorldPosition);
                if (dist == 0) return;

                //snap directly to plane if lower or higher than a threshold
                if (Mathf.Abs (dist) <= chunk.voxelScale) v.density = -dist;
                //increase or decrease density relative to the plane distance
                else if (dist < -chunk.voxelScale) {
                    v.density += intensity * Mathf.Abs (dist);
                } else if (dist > chunk.voxelScale) {
                    v.density -= intensity * Mathf.Abs (dist);
                }

                chunk.voxels.SetVoxel (voxelCoord, v);
            }
            chunk.needsUpdate = true;
        });
    }

    public override void ToolEnd (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, int falloff) {
        throw new System.NotImplementedException ();
    }

    public override void ToolStart (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, int falloff) {
        throw new System.NotImplementedException ();
    }
}