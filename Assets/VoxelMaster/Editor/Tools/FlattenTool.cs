using System.Collections.Generic;
using UnityEngine;
using VoxelMaster.Chunk;

public class FlattenTool : VoxelTool {
    public override string name => "Flatten Terrain";

    public override void OnToolGUI () {
        throw new System.NotImplementedException ();
    }

    public override void ToolDrag (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius) {
        throw new System.NotImplementedException ();
    }

    public override void ToolEnd (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius) {
        throw new System.NotImplementedException ();
    }

    public override void ToolStart (IVoxelData volume, VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius) {
        throw new System.NotImplementedException ();
    }
}