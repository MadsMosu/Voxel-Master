using UnityEngine;
using VoxelMaster.Chunk;

public class SmoothTool : VoxelTool {
    public override string name => "Smooth Terrain";
    private float squaredRadius;

    public override void OnToolGUI () { }
    public override void ToolStart (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        squaredRadius = Mathf.Sqrt (radius);
    }

    public override void ToolDrag (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        Vector3Int chunkWorldPosition = chunk.coords * (chunk.size - Vector3Int.one);
        Voxel[] smoothedVoxels = new Voxel[chunk.voxels.ToArray ().Length];

        chunk.voxels.Traverse ((x, y, z, v) => {
            Vector3Int voxelCoord = new Vector3Int (x, y, z);
            Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
            if (
                (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
                (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
            ) {

            }
        });

    }

    public override void ToolEnd (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
        // Do nothing
    }

    private float Smooth (VoxelChunk chunk, Vector3Int voxelCoord) {
        int i = 0;
        float sumDensity = 0f;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++) {
                    if (
                        (voxelCoord.x + x < 0 || voxelCoord.x + x >= chunk.size.x) ||
                        (voxelCoord.y + y < 0 || voxelCoord.y + y >= chunk.size.y) ||
                        (voxelCoord.z + z < 0 || voxelCoord.z + z >= chunk.size.z)
                    ) continue;
                    Vector3Int neighborCoord = new Vector3Int (x, y, z);
                    float squaredDistance = Vector3.SqrMagnitude (voxelCoord + neighborCoord);
                    if (squaredDistance <= squaredRadius) {
                        for (int nx = -1; nx <= 1; nx++)
                            for (int ny = -1; ny <= 1; ny++)
                                for (int nz = -1; nz <= 1; nz++) {
                                    int degree = Mathf.Abs (nx) + Mathf.Abs (ny) + Mathf.Abs (nz);

                                }
                    }
                    int neighborIndex = Util.Map3DTo1D (neighborCoord + Vector3Int.one + Vector3Int.one, 5);
                    // Debug.Log (chunk.voxels.GetVoxel (voxelCoord + neighborCoord).density);
                    sumDensity += chunk.voxels.GetVoxel (voxelCoord + neighborCoord).density;
                    i++;
                }
        return sumDensity / i;
    }
}