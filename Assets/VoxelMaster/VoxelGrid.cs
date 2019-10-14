using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using System.Collections;
using System.Linq;

namespace VoxelMaster
{
    public class VoxelGrid : MonoBehaviour
    {
        private ChunkDataStructure chunks = new ChunkDictionary();
        public Transform viewer;
        private Vector3Int viewerCoords = Vector3Int.zero;
        public int ChunkSize = 16;
        public int searchRadius = 6;

        public TerrainGraph terrainGraph;
        private ChunkMeshGenerator meshGenerator = new MarchingCubesMeshGenerator();


        void Start()
        {




        }

        void Update()
        {
            CreateNearbyChunks();

        }


        int genThisFrame = 0;
        void CreateNearbyChunks()
        {
            viewerCoords = new Vector3Int(
                Mathf.FloorToInt(viewer.position.x / ChunkSize),
                Mathf.FloorToInt(viewer.position.y / ChunkSize),
                Mathf.FloorToInt(viewer.position.z / ChunkSize)
            );


            for (int y = 0; y < searchRadius; y++)
            {
                int x = 0;
                int z = 0;
                int dx = 0;
                int dy = -1;
                for (int i = 0; i < (searchRadius * 2) * (searchRadius * 2); i++)
                {
                    if ((-searchRadius < x && x <= searchRadius) && ((-searchRadius < z && z <= searchRadius)))
                    {
                        var chunkCoords = viewerCoords + new Vector3Int(x, y, z);
                        if (!chunks.ChunkExists(chunkCoords))
                        {
                            var chunk = new Chunk(chunkCoords, ChunkSize);
                            chunks.AddChunk(chunkCoords, chunk);
                            // Debug.Log($"added chunk at {x},{y},{z}");
                            var job = new GenerateChunkDensityJob
                            {
                                chunkCoords = chunkCoords,
                                chunkSize = ChunkSize,
                                densities = chunk.Voxels,
                            };
                            job.Schedule(chunk.Voxels.Length, 64).Complete();
                            genThisFrame++;
                            if (genThisFrame > 32)
                            {
                                genThisFrame = 0;
                                return;
                            }
                        }
                    }
                    if (x == z || (x < 0 && x == -z) || (x > 0 && x == 1 - z))
                    {
                        var oldDx = dx;
                        dx = -dy;
                        dy = oldDx;
                    }
                    x += dx;
                    z += dy;
                }
            }

        }


        [BurstCompile]
        struct GenerateChunkDensityJob : IJobParallelFor
        {
            public NativeArray<byte> densities;
            public int chunkSize;
            public Vector3Int chunkCoords;
            public void Execute(int index)
            {
                densities[index] = (byte)(noise.cnoise(new float2(index, index)) * 255);
            }
        }


        void OnDestroy()
        {
            chunks.ForEach(c =>
            {
                c.Voxels.Dispose();
            });
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

    public class Spiral
    {
        public int x = 0;
        public int y = 0;
        public void Next()
        {
            if (x == 0 && y == 0)
            {
                x = 1;
                return;
            }
            if (Mathf.Abs(x) > Mathf.Abs(y) + 0.5f * Mathf.Sign(x) && Mathf.Abs(x) > (-y + 0.5f))
                y += (int)Mathf.Sign(x);
            else
                x -= (int)Mathf.Sign(y);
        }
        public Vector2 NextPoint()
        {
            Next();
            return new Vector2(x, y);
        }
        public void Reset()
        {
            x = 0;
            y = 0;
        }
    }
}
