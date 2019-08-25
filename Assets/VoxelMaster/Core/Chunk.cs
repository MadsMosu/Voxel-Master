

public class Chunk
{
    Voxel[,,] voxels = new Voxel[16, 16, 16];


    public void SetVoxel(int x, int y, int z, Voxel v)
    {
        Voxel test = new Voxel()
        {
            density = 0,
            type = 3
        };
    }
}
