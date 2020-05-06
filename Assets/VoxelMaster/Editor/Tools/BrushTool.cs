using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;
using VoxelMaster.Core.Rendering;

public class BrushTool : VoxelTool {
    public override string name => "Add / Remove Density";

    private bool inverse = false;

    public override void OnToolGUI () {
        GUILayout.Space (20);
        GUILayout.BeginVertical ();
        inverse = EditorGUILayout.Toggle ("Inverse", inverse);
        GUILayout.EndVertical ();
    }

    public override void ToolDrag (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        if (inverse) intensity = 0 - intensity;
        Vector3Int chunkWorldPosition = chunk.coords * (chunk.size - Vector3Int.one);
        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
            //if within radius
            if (
                (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
                (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
            ) {
                float tempIntensity = intensity;
                if (falloff > 0) {
                    float scaleFactor = Vector3.Distance (voxelWorldPosition, position) * falloff;
                    tempIntensity /= scaleFactor;
                }

                v.density += tempIntensity;
                chunk.voxels.SetVoxel (voxelCoord, v);
            }
        });
        ChunkRenderer.instance.RequestMesh (chunk.coords);
        GameObject go = voxelWorld.gameObjects[chunk.coords];
        go.GetComponent<MeshCollider> ().sharedMesh = ChunkRenderer.instance.GetChunkMesh (chunk.coords);
    }

    public override void ToolEnd (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) { }

    public override void ToolStart (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) { }
}