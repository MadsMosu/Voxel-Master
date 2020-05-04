using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelMaster.Chunk;

public class BrushTool : VoxelTool {
    public override string name => "Add / Remove Density";

    private bool inverse = false;

    public override void OnToolGUI () {
        GUILayout.Space (20);
        GUILayout.BeginVertical ();
        inverse = EditorGUILayout.Toggle ("Inverse", inverse);
        GUILayout.EndVertical ();
    }

    public override void ToolDrag (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, int falloff) {
        if (inverse) intensity = 0 - intensity;
        Vector3Int chunkWorldPosition = chunk.coords * chunk.size;
        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
            //if within radius
            if (
                (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
                (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
            ) {
                //if outside falloff
                if (voxelWorldPosition.x > position.x + falloff && voxelWorldPosition.y > position.y + falloff && voxelWorldPosition.z > position.z + falloff) {
                    //decrease intensity relative to the voxel pos between falloff and radius
                    Vector3 directionVector = (voxelWorldPosition - position).normalized;
                    directionVector *= falloff;
                    Vector3 closestPoint = directionVector + position;
                    intensity *= ((radius - Vector3.Distance (voxelWorldPosition, closestPoint)) / falloff);
                }

                v.density += intensity;
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