using UnityEngine;

[System.Serializable]
public class SimpleDataStructure : VoxelDataStructure
{
    private Vector3Int dimensions;
    private Voxel[] voxels;
    private int Map3DTo1D(Vector3Int coords)
    {
        return coords.x + dimensions.y * (coords.y + dimensions.z * coords.z);
    }

    public override void Init(Vector3Int dimensions)
    {
        this.dimensions = dimensions;
        this.voxels = new Voxel[dimensions.x * dimensions.y * dimensions.z];
    }

    public override Voxel GetVoxel(Vector3Int coords)
    {
        return voxels[Map3DTo1D(coords)];
    }

    public override void SetVoxel(Vector3Int coords, Voxel voxel)
    {
        voxels[Map3DTo1D(coords)] = voxel;
    }
}
