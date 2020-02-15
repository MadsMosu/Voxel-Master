using UnityEngine;

[System.Serializable]
public class SimpleDataStructure : VoxelDataStructure
{
    private Vector3Int size;
    private Voxel[] voxels;


    public override void Init(Vector3Int size)
    {
        this.size = size;
        this.voxels = new Voxel[size.x * size.y * size.z];
    }

    public override Voxel GetVoxel(Vector3Int coords)
    {
        return voxels[Util.Map3DTo1D(coords, size)];
    }

    public override void SetVoxel(Vector3Int coords, Voxel voxel)
    {
        voxels[Util.Map3DTo1D(coords, size)] = voxel;
    }
}
