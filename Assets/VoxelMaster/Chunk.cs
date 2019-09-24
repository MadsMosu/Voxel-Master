using UnityEngine;

public class Chunk
{
    public Vector3Int Coords { get; private set; }
    public Voxel[] Voxels { get; private set; }
    public int Size { get; private set; }

    public enum ChunkStatus
    {
        CREATED, GENERATED_DATA, GENERATED_MESH
    }

    public ChunkStatus Status = ChunkStatus.CREATED;

    public Chunk(Vector3Int coords, int size)
    {
        this.Coords = coords;
        this.Size = size;
        this.Voxels = new Voxel[size * size * size];

    }



}