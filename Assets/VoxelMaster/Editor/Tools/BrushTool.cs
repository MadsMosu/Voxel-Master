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

    public override void ToolDrag (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius) {
        if (inverse) intensity = 0 - intensity;
        Vector3Int chunkWorldPosition = chunk.coords * chunk.size;
        chunk.voxels.Traverse ((x, y, z, v) => {

        });

    }

    public override void ToolEnd (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius) {
        throw new System.NotImplementedException ();
    }

    public override void ToolStart (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius) {
        throw new System.NotImplementedException ();
    }
}