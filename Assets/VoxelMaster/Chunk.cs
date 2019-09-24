using UnityEngine;

public class Chunk
{
    public Vector3Int coords {get; private set;}
    private Voxel[] voxels;
    private int size;
    public Chunk(Vector3Int coords, int size)
    {
        this.coords = coords;
        this.size = size;
        this.voxels = new Voxel[size * size * size];
    }



}