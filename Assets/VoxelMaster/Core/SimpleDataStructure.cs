using UnityEngine;

[System.Serializable]
public class SimpleDataStructure : VoxelDataStructure
{
    private Vector3Int size;
    private Voxel[] voxels;
    private int Map3DTo1D(Vector3Int coords)
    {
        return coords.x + size.y * (coords.y + size.z * coords.z);
    }

    public override void Init(Vector3Int size)
    {
        this.size = size;
        this.voxels = new Voxel[size.x * size.y * size.z];
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
