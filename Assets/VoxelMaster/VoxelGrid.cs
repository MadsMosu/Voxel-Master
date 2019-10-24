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


        public Material material;
        public MeshFilter testMeshFilter;

        public Dictionary<Chunk, Mesh> chunkMeshes = new Dictionary<Chunk, Mesh>();

        void Start()
        {
        }

        void Update()
        {
            CreateNearbyChunks();

            RenderChunks();
        }

        private void RenderChunks()
        {
            foreach (KeyValuePair<Chunk, Mesh> entry in chunkMeshes)
            {
                Graphics.DrawMesh(entry.Value, entry.Key.Coords * (entry.Key.Size - 1), Quaternion.identity, material, 0);
            }
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
                                chunkSize = chunk.Size,
                                densities = chunk.Voxels,
                            };
                            var densityJob = job.Schedule(chunk.Voxels.Length, 64);

                            var triangles = new NativeArray<Triangle>(5 * chunk.Voxels.Length, Allocator.TempJob);
                            var meshJob = new GenerateChunkMeshJob
                            {
                                chunkSize = chunk.Size,
                                densities = chunk.Voxels,
                                isoLevel = .5f,
                                triangles = triangles,
                                triTable = NativeLookup.triTabl,
                                cornerIndexAFromEdge = NativeLookup.cornerIndexAFromEdge,
                                cornerIndexBFromEdge = NativeLookup.cornerIndexBFromEdge
                            };
                            var meshJobHandle = meshJob.Schedule(ChunkSize*ChunkSize*ChunkSize, 64, densityJob);
                            // densityJob.Complete();
                            // meshJob.Run();
                            meshJobHandle.Complete();


                            var vertices = new List<Vector3>();
                            var triangleIndices = new List<int>();
                            int triIndex = 0;
                            foreach (var triangle in triangles)
                            {
                                if (!triangle.isTriangle) continue;
                                vertices.Add(triangle.a);
                                vertices.Add(triangle.b);
                                vertices.Add(triangle.c);

                                triangleIndices.Add(triIndex++);
                                triangleIndices.Add(triIndex++);
                                triangleIndices.Add(triIndex++);
                            }

                            var mesh = new Mesh();
                            mesh.SetVertices(vertices);
                            mesh.SetTriangles(triangleIndices, 0);
                            mesh.RecalculateNormals();
                            chunkMeshes.Add(chunk, mesh);

                            triangles.Dispose();


                            genThisFrame++;
                            if (genThisFrame > 16)
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
            public NativeArray<float> densities;
            public int chunkSize;
            public Vector3Int chunkCoords;

            public void Execute(int index)
            {
                var voxelCoord = (chunkCoords * (chunkSize - 1)) + Util.Map1DTo3D(index, chunkSize);

                var n = noise.cnoise(new float3(
                    voxelCoord.x,
                    voxelCoord.y,
                    voxelCoord.z) / 50.00f
                );

                densities[index] = n * 0.5f + 0.5f;
                // densities[index] = math.unlerp(-1, 1, n);

            }
        }


        [BurstCompile]
        struct GenerateChunkMeshJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> densities;
            [ReadOnly] public int chunkSize;
            [ReadOnly] public float isoLevel;
            [NativeDisableParallelForRestriction] public NativeArray<Triangle> triangles;
            [ReadOnly] public NativeArray<int> triTable;
            [ReadOnly] public NativeArray<int> cornerIndexAFromEdge;
            [ReadOnly] public NativeArray<int> cornerIndexBFromEdge;


            int triangleIndex;

            public void Execute(int index)
            {
                var coords = Util.Map1DTo3D(index, chunkSize - 1);
                var x = coords.x; var y = coords.y; var z = coords.z;
                index = Util.Map3DTo1D(x, y, z, chunkSize);

                var cubeDensity = new NativeArray<float>(8, Allocator.Temp);
                cubeDensity[0] = densities[Util.Map3DTo1D(x, y, z, chunkSize)];
                cubeDensity[1] = densities[Util.Map3DTo1D((x + 1), y, z, chunkSize)];
                cubeDensity[2] = densities[Util.Map3DTo1D((x + 1), y, (z + 1), chunkSize)];
                cubeDensity[3] = densities[Util.Map3DTo1D(x, y, (z + 1), chunkSize)];
                cubeDensity[4] = densities[Util.Map3DTo1D(x, (y + 1), z, chunkSize)];
                cubeDensity[5] = densities[Util.Map3DTo1D((x + 1), (y + 1), z, chunkSize)];
                cubeDensity[6] = densities[Util.Map3DTo1D((x + 1), (y + 1), (z + 1), chunkSize)];
                cubeDensity[7] = densities[Util.Map3DTo1D(x, (y + 1), (z + 1), chunkSize)];

                var cubeVectors = new NativeArray<float3>(8, Allocator.Temp);
                cubeVectors[0] = new float3(x, y, z);
                cubeVectors[1] = new float3((x + 1), y, z);
                cubeVectors[2] = new float3((x + 1), y, (z + 1));
                cubeVectors[3] = new float3(x, y, (z + 1));
                cubeVectors[4] = new float3(x, (y + 1), z);
                cubeVectors[5] = new float3((x + 1), (y + 1), z);
                cubeVectors[6] = new float3((x + 1), (y + 1), (z + 1));
                cubeVectors[7] = new float3(x, (y + 1), (z + 1));

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
                var triangleIndex = 0;
                for (int i = currentTriangulationIndex; triTable[i] != -1; i += 3)
                {
                    var triVerts = new NativeArray<Vector3>(3, Allocator.Temp);
                    for (int j = 0; j < 3; j++)
                    {
                        var a0 = cornerIndexAFromEdge[triTable[i + j]];
                        var b0 = cornerIndexBFromEdge[triTable[i + j]];
                        triVerts[j] = Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0]));
                    }

                    triangles[index * 5 + triangleIndex++] = new Triangle
                    {
                        isTriangle = true,
                        a = triVerts[0],
                        b = triVerts[1],
                        c = triVerts[2],
                    };
                    triVerts.Dispose();
                }
                cubeDensity.Dispose();
                cubeVectors.Dispose();

            }

        }

        public struct Triangle
        {
            public bool isTriangle;
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;
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
}
