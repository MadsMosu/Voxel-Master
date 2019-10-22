using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelMaster;

public class JobSystemTester : MonoBehaviour
{

    public float3[] verts;
    Chunk testChunk;

    // Start is called before the first frame update
    void Start()
    {
        var ChunkSize = 4;
        var densities = new NativeArray<float>(ChunkSize * ChunkSize * ChunkSize, Allocator.TempJob);


        var densityJob = new GenerateChunkDensityJob
        {
            chunkCoords = new Vector3Int(42, 62, 23),
            chunkSize = ChunkSize,
            densities = densities
        };

        var densityJobHandle = densityJob.Schedule(ChunkSize * ChunkSize * ChunkSize, 64);

        var triangles = new NativeArray<Triangle>(5 * ((ChunkSize - 1) * (ChunkSize - 1) * (ChunkSize - 1)), Allocator.TempJob);
        var marchingCube = new GenerateChunkMeshJob
        {
            densities = densities,
            isoLevel = .5f,
            triangles = triangles,
            triTable = NativeLookup.triTabl,
            cornerIndexAFromEdge = NativeLookup.cornerIndexAFromEdge,
            cornerIndexBFromEdge = NativeLookup.cornerIndexBFromEdge
        };

        var readJobHandle = marchingCube.Schedule(ChunkSize * ChunkSize * ChunkSize, 32, densityJobHandle);



        readJobHandle.Complete();

        Debug.Log("trutle");


    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDrawGizmos()
    {
        if (verts != null)
            for (int i = 0; i < verts.Length; i++)
            {
                Gizmos.DrawSphere(verts[i], .05f);
            }
    }



    struct GenerateChunkDensityJob : IJobParallelFor
    {
        public NativeArray<float> densities;
        public int chunkSize;
        public Vector3Int chunkCoords;
        public void Execute(int index)
        {
            var voxelCoord = (chunkCoords * (chunkSize + 1)) + Util.Map1DTo3D(index, chunkSize);

            var n = noise.cnoise(new float3(
                voxelCoord.x,
                voxelCoord.y,
                voxelCoord.z) / 5.00f
            );

            densities[index] = math.unlerp(-1, 1, n);

        }
    }

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

        int Map3DTo1D(int x, int y, int z, int size)
        {
            return x + size * (y + size * z);
        }

        public void Execute(int index)
        {
            var coords = Util.Map1DTo3D(index, chunkSize);
            var x = coords.x; var y = coords.y; var z = coords.z;

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
            for (int i = currentTriangulationIndex; triTable[i] != -1; i += 3)
            {
                var triVerts = new NativeArray<Vector3>(3, Allocator.Temp);
                for (int j = 0; j < 3; j++)
                {
                    var a0 = cornerIndexAFromEdge[triTable[i + j]];
                    var b0 = cornerIndexBFromEdge[triTable[i + j]];
                    // vertices.Add(Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0])));
                    triVerts[j] = Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0]));
                }

                triangles[index + (index % 5)] = new Triangle
                {
                    isTriangle = true,
                    a = triVerts[0],
                    b = triVerts[1],
                    c = triVerts[2],
                };


            }


        }

    }

    public struct Triangle
    {
        public bool isTriangle;
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
    }

}
