using System.Collections.Generic;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;
using VoxelMaster.Core.Rendering;

public class FlattenTool : VoxelTool {
    public override string name => "Flatten Terrain";
    Plane plane;

    public override void OnToolGUI () {
        throw new System.NotImplementedException ();
    }

    public override void ToolDrag (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        Vector3Int chunkWorldPosition = chunk.coords * chunk.size;
        if (plane.ToString ().Equals (default (Plane).ToString ())) {
            plane = new Plane (surfaceNormal, position);
        }

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
                // if (Mathf.Abs (dist) <= chunk.voxelScale) v.density = -dist;
                //increase or decrease density relative to the plane distance
                else if (dist < 0) {
                    v.density += intensity;
                } else if (dist > 0) {
                    v.density -= intensity;
                }

                chunk.voxels.SetVoxel (voxelCoord, v);
            }
        });
        ChunkRenderer.instance.RequestMesh (chunk.coords);
        GameObject go = voxelWorld.gameObjects[chunk.coords];
        go.GetComponent<MeshCollider> ().sharedMesh = ChunkRenderer.instance.GetChunkMesh (chunk.coords);
    }

    public override void ToolEnd (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        plane = new Plane ();
    }

    public override void ToolStart (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        throw new System.NotImplementedException ();
    }
}