using System.Collections.Generic;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;
using VoxelMaster.Core.Rendering;

public class FlattenTool : VoxelTool {
    public override string name => "Flatten Terrain";
    Plane plane;
    bool planeLocked = false;

    public override void OnToolGUI () { }

    public override void ToolDrag (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        Vector3Int chunkWorldPosition = chunk.coords * (chunk.size - Vector3Int.one);

        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
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
                v.density = Mathf.MoveTowards (v.density, dist, tempIntensity);

                chunk.voxels.SetVoxel (voxelCoord, v);
            }
        });
        ChunkRenderer.instance.RequestMesh (chunk.coords);
        GameObject go = voxelWorld.gameObjects[chunk.coords];
        go.GetComponent<MeshCollider> ().sharedMesh = ChunkRenderer.instance.GetChunkMesh (chunk.coords);
    }

    public override void ToolEnd (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        planeLocked = false;
    }

    public override void ToolStart (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        if (planeLocked == false)
            plane = new Plane (surfaceNormal, position);
        planeLocked = true;
    }
}