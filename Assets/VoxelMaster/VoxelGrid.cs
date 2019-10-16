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

        private NativeArray<int> triTable;

        void Start()
        {
            triTable = new NativeArray<int>(triTable.Length * 16, Allocator.Persistent);


            for (int i = 0; i < Lookup.triTabl.Length; i++)
            {
                for (int j = 0; j < Lookup.triTabl[i].Length; j++)
                {
                    triTable[i * 16 + j] = Lookup.triTabl[i][j];
                }
            }



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


            for (int y = -searchRadius; y < searchRadius; y++)
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

                            var vertices = new NativeArray<Vector3>(16 * (chunk.Voxels.Length - chunk.Size), Allocator.TempJob);
                            var triangles = new NativeArray<int>(1, Allocator.TempJob);
                            var meshJob = new GenerateChunkMeshJob
                            {
                                chunkSize = ChunkSize,
                                densities = chunk.Voxels,
                                isoLevel = .5f,
                                vertices = vertices,
                                triangles = triangles,
                                triTable = triTable
                            };
                            var meshJobHandle = meshJob.Schedule();
                            meshJobHandle.Complete();
                            vertices.Dispose();
                            triangles.Dispose();


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


        [BurstCompile]
        struct GenerateChunkMeshJob : IJob
        {

            public NativeArray<byte> densities;
            public int chunkSize;
            public float isoLevel;
            public NativeArray<Vector3> vertices;
            public NativeArray<int> triangles;


            public NativeArray<int> triTable;
            int currentVertexIndex;

            int Map3DTo1D(int x, int y, int z, int size)
            {
                return x + size * (y + size * z);
            }

            public void Execute()
            {
                if (isoLevel == 0)
                {
                    isoLevel = .5f;
                }

                for (int x = 0; x < chunkSize - 1; x++)
                    for (int y = 0; y < chunkSize - 1; y++)
                        for (int z = 0; z < chunkSize - 1; z++)
                        {

                            var cubeDensity = new NativeArray<float>(8, Allocator.Temp);
                            cubeDensity[0] = densities[Util.Map3DTo1D(x, y, z, chunkSize)];
                            cubeDensity[1] = densities[Util.Map3DTo1D((x + 1), y, z, chunkSize)];
                            cubeDensity[2] = densities[Util.Map3DTo1D((x + 1), y, (z + 1), chunkSize)];
                            cubeDensity[3] = densities[Util.Map3DTo1D(x, y, (z + 1), chunkSize)];
                            cubeDensity[4] = densities[Util.Map3DTo1D(x, (y + 1), z, chunkSize)];
                            cubeDensity[5] = densities[Util.Map3DTo1D((x + 1), (y + 1), z, chunkSize)];
                            cubeDensity[6] = densities[Util.Map3DTo1D((x + 1), (y + 1), (z + 1), chunkSize)];
                            cubeDensity[7] = densities[Util.Map3DTo1D(x, (y + 1), (z + 1), chunkSize)];


                            var cubeVectors = new NativeArray<Vector3>(8, Allocator.Temp);
                            cubeVectors[0] = new Vector3(x, y, z);
                            cubeVectors[1] = new Vector3((x + 1), y, z);
                            cubeVectors[2] = new Vector3((x + 1), y, (z + 1));
                            cubeVectors[3] = new Vector3(x, y, (z + 1));
                            cubeVectors[4] = new Vector3(x, (y + 1), z);
                            cubeVectors[5] = new Vector3((x + 1), (y + 1), z);
                            cubeVectors[6] = new Vector3((x + 1), (y + 1), (z + 1));
                            cubeVectors[7] = new Vector3(x, (y + 1), (z + 1));

                            int cubeindex = 0;
                            if (cubeDensity[0] < isoLevel) cubeindex |= 1;
                            if (cubeDensity[1] < isoLevel) cubeindex |= 2;
                            if (cubeDensity[2] < isoLevel) cubeindex |= 4;
                            if (cubeDensity[3] < isoLevel) cubeindex |= 8;
                            if (cubeDensity[4] < isoLevel) cubeindex |= 16;
                            if (cubeDensity[5] < isoLevel) cubeindex |= 32;
                            if (cubeDensity[6] < isoLevel) cubeindex |= 64;
                            if (cubeDensity[7] < isoLevel) cubeindex |= 128;

                            var currentTriangulationIndex = cubeindex * 16;
                            for (int i = currentTriangulationIndex; triTable[i] != -1; i += 3)
                            {

                                var points = new Vector3[3];

                                for (int j = 0; j < 3; j++)
                                {
                                    var a0 = Lookup.cornerIndexAFromEdge[triTable[i + j]];
                                    var b0 = Lookup.cornerIndexBFromEdge[triTable[i + j]];
                                    // points[j] = Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0]));
                                    vertices[currentVertexIndex++] = (Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0])));
                                    // var currentTriangleIndex = vertices.Length;
                                    // triangles.Add(currentTriangleIndex);
                                }

                            }



                        }
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
