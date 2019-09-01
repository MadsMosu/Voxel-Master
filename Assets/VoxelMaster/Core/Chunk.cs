

using UnityEngine;

public class Chunk
{

    public Vector3Int chunkCoordinates;
    public int size;
    public int lod;


    private Voxel[,,] voxels;

    public Voxel[,,] Voxels
    {
        get { return voxels; }
    }

    public int LOD
    {
        get { return lod; }
        set { lod = value;  }
    }


    public Chunk(Vector3Int coordinates, int size, int lod)
    {
        chunkCoordinates = coordinates;
        this.size = size;
        this.lod = lod;
    }


    public void SetVoxel(Vector3Int p, Voxel v)
    {
        voxels[p.x, p.y, p.z] = v;
    }


    public void SetVoxels(Voxel[,,] voxels)
    {
        this.voxels = voxels;
    }

    //used for getting the index of a 1D array
    //public int Index(int x, int y, int z)
    //{
    //    return (x + size * (y + size * z));
    //}

    public void AddDensityInSphere(Vector3 origin, float radius, float falloff)
    {
        //TODO:
    }
}
