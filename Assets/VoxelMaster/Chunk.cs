using UnityEngine;

public class Chunk
{
    public Vector3Int Coords { get; private set; }
    public Voxel[] Voxels { get; private set; }
    public int Size { get; private set; }

    public enum ChunkStatus
    {
        CREATED, GENERATING, GENERATED_DATA, GENERATED_MESH
    }

    public ChunkStatus Status = ChunkStatus.CREATED;

    public Chunk(Vector3Int coords, int size)
    {
        this.Coords = coords;
        this.Size = size;
    }


    public void InitVoxels()
    {
        this.Voxels = new Voxel[Size * Size * Size];
    }


}