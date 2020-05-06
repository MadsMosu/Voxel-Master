using UnityEngine;
using VoxelMaster.Chunk;

public class MaterialTool : VoxelTool {
    public VoxelMaterial material { get; private set; }
    public byte materialIndex { get; private set; }

    public override string name => "Set Material";

    public override void OnToolGUI () {
        throw new System.NotImplementedException ();
    }

    public override void ToolDrag (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        throw new System.NotImplementedException ();
    }

    public override void ToolEnd (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        throw new System.NotImplementedException ();
    }

    public override void ToolStart (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        throw new System.NotImplementedException ();
    }
}