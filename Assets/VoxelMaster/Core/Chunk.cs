

using UnityEngine;

public class Chunk
{

    public Vector3Int chunkCoordinates;

    private Voxel[,,] voxels;
    private Chunk[] neighborChunks;

    public Voxel[,,] Voxels
    {
        get { return voxels; }
    }

    public Chunk[] NeighborChunks
    {
        get { return neighborChunks; }
    }




    public Chunk(Vector3Int coordinates, int size)
    {
        chunkCoordinates = coordinates;

        voxels = new Voxel[size + 1, size + 1, size + 1];
    }


    public void SetVoxel(Vector3Int p, Voxel v)
    {
        voxels[p.x, p.y, p.z] = v;
    }

    //public int Index(int x, int y, int z)
    //{
    //    return (x + size * (y + size * z));
    //}

    public void AddDensityInSphere(Vector3 origin, float radius, float falloff)
    {
        //TODO:
    }
}
