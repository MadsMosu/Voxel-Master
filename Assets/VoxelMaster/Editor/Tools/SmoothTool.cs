using UnityEngine;
using VoxelMaster.Chunk;

public class SmoothTool : VoxelTool {
    public override string name => "Smooth Terrain";

    public override void OnToolGUI () { }
    public override void ToolStart (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {

    }

    public override void ToolDrag (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        Vector3Int chunkWorldPosition = chunk.coords * (chunk.size - Vector3Int.one);
        float[] avgDensities = new float[chunk.voxels.ToArray ().Length];

        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
            if (
                (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
                (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
            ) {
                avgDensities[Util.Map3DTo1D (voxelCoord, chunk.size)] = GetAvgVoxelDensity (chunk, v, voxelCoord);
            }
        });

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
                float avgDensity = avgDensities[Util.Map3DTo1D (voxelCoord, chunk.size)];
                if (Mathf.Abs (avgDensity - v.density) < 0.30f) return;
                else v.density = Mathf.MoveTowards (v.density, avgDensity, tempIntensity);

                chunk.voxels.SetVoxel (voxelCoord, v);
            }
        });
    }

    public override void ToolEnd (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        // Do nothing
    }

    private float GetAvgVoxelDensity (VoxelChunk chunk, Voxel voxel, Vector3Int voxelCoord) {
        var sumDensity = 0f;
        int i = 0;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++) {
                    if (
                        (voxelCoord.x + x < 0 || voxelCoord.x + x >= chunk.size.x) ||
                        (voxelCoord.y + y < 0 || voxelCoord.y + y >= chunk.size.y) ||
                        (voxelCoord.z + z < 0 || voxelCoord.z + z >= chunk.size.z)
                    ) continue;
                    // if (x != 0 && y != 0 || y != 0 && z != 0 || z != 0 && x != 0) continue;
                    if (x == 0 || y == 0 || z == 0) continue;

                    Vector3Int neighborCoord = voxelCoord + new Vector3Int (x, y, z);

                    float neighborDensity = chunk.voxels.GetVoxel (neighborCoord).density;
                    sumDensity += neighborDensity;
                    // / Vector3.Distance (voxelCoord, neighborCoord)
                    i++;
                }

        return sumDensity / i;
    }
}