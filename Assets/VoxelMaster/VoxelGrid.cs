using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public class VoxelGrid : MonoBehaviour
{
    private ChunkDataStructure chunks = new ChunkDictionary();
    public Transform viewer;
    private Vector3Int viewerCoords = Vector3Int.zero;
    public int ChunkSize = 16;
    public int searchRadius = 6;

    public TerrainGraph terrainGraph;
    private ChunkMeshGenerator meshGenerator = new MarchingCubesMeshGenerator();

    private Queue<Chunk> chunkGenerationQueue = new Queue<Chunk>();
    private Thread chunkQueueProcessor;

    void Start()
    {

        ThreadPool.SetMaxThreads(4, 4);


        chunkQueueProcessor = new Thread(new ThreadStart(
            ProcessChunkGenerationQueue
        ));
        chunkQueueProcessor.IsBackground = true;
        chunkQueueProcessor.Priority = System.Threading.ThreadPriority.Lowest;
        chunkQueueProcessor.Start();
    }

    void Update()
    {
        CreateNearbyChunks();
    }

    void CreateNearbyChunks()
    {
        viewerCoords = new Vector3Int(
            Mathf.FloorToInt(viewer.position.x / ChunkSize),
            Mathf.FloorToInt(viewer.position.y / ChunkSize),
            Mathf.FloorToInt(viewer.position.z / ChunkSize)
        );


        for (int x = -searchRadius; x < searchRadius; x++)
            for (int y = -searchRadius; y < searchRadius; y++)
                for (int z = -searchRadius; z < searchRadius; z++)
                {
                    var chunkCoords = viewerCoords + new Vector3Int(x, y, z);
                    if (chunks.GetChunk(chunkCoords) == null)
                    {
                        var chunk = new Chunk(chunkCoords, ChunkSize);
                        chunks.AddChunk(chunkCoords, chunk);
                        lock (chunkGenerationQueue)
                        {
                            chunkGenerationQueue.Enqueue(chunk);
                        }
                    }
                }

    }

    void ProcessChunkGenerationQueue()
    {
        while (true)
        {
            lock (chunkGenerationQueue)
            {
                if (chunkGenerationQueue.Count > 0)
                {
                    var chunk = chunkGenerationQueue.Dequeue();

                    var cb = new WaitCallback(GenerateChunkDensity);
                    ThreadPool.QueueUserWorkItem(cb, chunk);
                    Thread.Sleep(1);
                }
            }
        }
    }


    private void GenerateChunkDensity(object obj)
    {
        var chunk = obj as Chunk;
        chunk.Status = Chunk.ChunkStatus.GENERATING;

        chunk.InitVoxels();
        for (int i = 0; i < chunk.Voxels.Length; i++)
        {
            // chunk.Voxels[i] = new Voxel();
            // chunk.Voxels[i].Density = (byte)(terrainGraph.Evaluate(
            //     Util.Map1DTo3D(i, chunk.Size) + chunk.Coords * chunk.Size
            // ) * 255);


        }

        chunk.Status = Chunk.ChunkStatus.GENERATED_DATA;

        var cb = new WaitCallback(GenerateChunkMesh);
        ThreadPool.QueueUserWorkItem(cb, chunk);
    }

    private void GenerateChunkMesh(object obj)
    {
        var chunk = obj as Chunk;
        meshGenerator.GenerateMesh(chunk);
        chunk.Status = Chunk.ChunkStatus.GENERATED_MESH;

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
                case Chunk.ChunkStatus.GENERATING:
                    Gizmos.color = Color.magenta;
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