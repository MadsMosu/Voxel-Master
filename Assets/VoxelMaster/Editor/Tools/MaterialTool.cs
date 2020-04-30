using UnityEngine;

public class MaterialTool : VoxelTool
{
    public VoxelMaterial material { get; private set; }
    public byte materialIndex { get; private set; }

    public override string name => "Set Material";

    public override void OnToolGUI() {
        throw new System.NotImplementedException();
    }

    public override void ToolDrag(IVoxelData voxels, Vector3 position, Vector3 surfaceNormal) {
        throw new System.NotImplementedException();
    }

    public override void ToolEnd(IVoxelData voxels, Vector3 position, Vector3 surfaceNormal) {
        throw new System.NotImplementedException();
    }

    public override void ToolStart(IVoxelData voxels, Vector3 position, Vector3 surfaceNormal) {
        throw new System.NotImplementedException();
    }
}
