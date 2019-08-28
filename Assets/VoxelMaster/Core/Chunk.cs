

using UnityEngine;

public class Chunk
{

    public Vector3Int chunkCoordinates;

    public Voxel[,,] Voxels
    {
        get { return voxels; }
    }

    private Voxel[,,] voxels;

    public Chunk(Vector3Int coordinates, int size)
    {

        chunkCoordinates = coordinates;

        voxels = new Voxel[size + 1, size + 1, size + 1];

        for (int x = 0; x < size + 1; x++)
            for (int y = 0; y < size + 1; y++)
                for (int z = 0; z < size + 1; z++)
                {
                    voxels[x, y, z].Density = Perlin.Noise(((chunkCoordinates.x * size) + x) * 0.05f, ((chunkCoordinates.y * size) + y) * 0.05f, ((chunkCoordinates.z * size) + z) * 0.05f) * 2;
                }
    }


    public void SetVoxel(Vector3Int p, Voxel v)
    {
        voxels[p.x, p.y, p.z] = v;
    }

    public void AddDensityInSphere(Vector3 origin, float radius, float falloff)
    {
        //TODO:
    }
}
