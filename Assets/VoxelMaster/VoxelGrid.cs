using System;
using System.Threading;
using UnityEngine;


public class VoxelGrid : MonoBehaviour
{
    private ChunkDataStructure chunks = new ChunkDictionary();
    public Transform viewer;
    public int ChunkSize = 16;

    public TerrainGraph terrainGraph;

    void Update()
    {
        GenerateNearbyChunks();
    }

    void GenerateNearbyChunks()
    {
        Vector3Int viewerCoordinate = new Vector3Int(
            Mathf.FloorToInt(viewer.position.x / ChunkSize),
            Mathf.FloorToInt(viewer.position.y / ChunkSize),
            Mathf.FloorToInt(viewer.position.z / ChunkSize)
        );
        if (chunks.GetChunk(viewerCoordinate) == null)
        {
            GenerateChunk(new Chunk(viewerCoordinate, ChunkSize));
        }
    }

    void GenerateChunk(Chunk chunk)
    {
        chunks.AddChunk(chunk.Coords, chunk);

        var cb = new WaitCallback(GenerateChunkDensity);
        ThreadPool.QueueUserWorkItem(cb, chunk);
    }

    private void GenerateChunkDensity(object obj)
    {
        var chunk = obj as Chunk;
        for (int i = 0; i < chunk.Voxels.Length; i++)
        {
            var voxelPosition = Util.Map1DTo3D(i, chunk.Size);
            voxelPosition += chunk.Coords * chunk.Size;

            chunk.Voxels[i] = new Voxel();
            chunk.Voxels[i].Density = terrainGraph.Evaluate(voxelPosition);
        }
        chunk.Status = Chunk.ChunkStatus.GENERATED_DATA;

        var cb = new WaitCallback(GenerateChunkMesh);
        ThreadPool.QueueUserWorkItem(cb, chunk);
    }

    private void GenerateChunkMesh(object obj)
    {
        var chunk = obj as Chunk;


    }

    void OnDrawGizmos()
    {
        chunks.ForEach(c =>
        {
            switch (c.Status)
            {
                case Chunk.ChunkStatus.CREATED:
                    Gizmos.color = Color.white;
                    break;
                case Chunk.ChunkStatus.GENERATED_DATA:
                    Gizmos.color = Color.blue;
                    break;
                case Chunk.ChunkStatus.GENERATED_MESH:
                    Gizmos.color = Color.green;
                    break;
                default:
                    Gizmos.color = Color.red;
                    break;
            }
            Gizmos.DrawWireCube((c.Coords * ChunkSize) + Vector3.one * (ChunkSize / 2), ChunkSize * Vector3.one);
        });
    }




}