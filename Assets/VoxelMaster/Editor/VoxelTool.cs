using System.Collections.Generic;
using UnityEngine;
using VoxelMaster.Chunk;

public abstract class VoxelTool {

    public abstract string name { get; }
    public abstract void ToolStart (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius);
    public abstract void ToolDrag (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius);
    public abstract void ToolEnd (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius);
    public abstract void OnToolGUI ();
}