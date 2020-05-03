
using UnityEngine;

public abstract class VoxelTool {

    public abstract string name { get; }
    public abstract void ToolStart(IVoxelData voxels, Vector3 position, Vector3 surfaceNormal);
    public abstract void ToolDrag(IVoxelData voxels, Vector3 position, Vector3 surfaceNormal);
    public abstract void ToolEnd(IVoxelData voxels, Vector3 position, Vector3 surfaceNormal);
    public abstract void OnToolGUI();
}