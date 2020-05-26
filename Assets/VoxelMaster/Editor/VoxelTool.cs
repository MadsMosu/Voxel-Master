using System.Collections.Generic;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;

public abstract class VoxelTool {

    public abstract string name { get; }
    public abstract void ToolStart (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, TestVoxelWorld voxelWorld);
    public abstract void ToolDrag (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, TestVoxelWorld voxelWorld);
    public abstract void ToolEnd (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff, TestVoxelWorld voxelWorld);
    public abstract void OnToolGUI ();
}