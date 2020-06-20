using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelMaster;
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

    public override void ToolDrag (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) {
        if (inverse) intensity = 0 - intensity;
        Vector3Int chunkWorldPosition = chunk.coords * (chunk.size);

        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
            //if within radius
            if (
                (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
                (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
            ) {
                chunk.dirty = true;
                float tempIntensity = intensity;
                if (falloff > 0) {
                    float scaleFactor = Vector3.Distance (voxelWorldPosition, position) * falloff;
                    tempIntensity /= scaleFactor;
                }

                v.density += tempIntensity * Time.deltaTime;
                chunk.voxels.SetVoxel (voxelCoord.x, voxelCoord.y, voxelCoord.z, v);
            }
        });

    }

    public override void ToolEnd (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) { }

    public override void ToolStart (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, VoxelWorld voxelWorld) { }
}