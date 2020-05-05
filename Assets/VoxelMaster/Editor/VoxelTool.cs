using System.Collections.Generic;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;

public abstract class VoxelTool {

    public abstract string name { get; }
    public abstract void ToolStart (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff);
    public abstract void ToolDrag (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff);
    public abstract void ToolEnd (VoxelWorld voxelWorld, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff);
    public abstract void OnToolGUI ();
}