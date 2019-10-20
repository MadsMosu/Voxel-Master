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
        var ChunkSize = 16;
        var vertices = new NativeList<float3>(15 * ((ChunkSize - 1) * (ChunkSize - 1) * (ChunkSize - 1)), Allocator.TempJob);
        var triangles = new NativeList<int>(5 * ((ChunkSize - 1) * (ChunkSize - 1) * (ChunkSize - 1)), Allocator.TempJob);

        var marchingCube = new MarchingCube
        {
            vertices = vertices,
            triangles = triangles,
            triTable = NativeLookup.triTabl,
            cornerIndexAFromEdge = NativeLookup.cornerIndexAFromEdge,
            cornerIndexBFromEdge = NativeLookup.cornerIndexBFromEdge
        };

        var readJobHandle = marchingCube.Schedule();


        readJobHandle.Complete();
        verts = vertices.ToArray();
        Debug.Log(vertices.Length);

        vertices.Dispose();
        triangles.Dispose();

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


    struct MarchingCube : IJob
    {
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        public NativeArray<int> triTable;
        public NativeArray<int> cornerIndexAFromEdge;
        public NativeArray<int> cornerIndexBFromEdge;


        int currentVertexIndex;
        public void Execute()
        {
            var cubeDensity = new NativeArray<float>(8, Allocator.Temp);
            cubeDensity[0] = 1f;
            cubeDensity[1] = 0f;
            cubeDensity[2] = 0f;
            cubeDensity[3] = .75f;
            cubeDensity[4] = 0f;
            cubeDensity[5] = 1f;
            cubeDensity[6] = 1f;
            cubeDensity[7] = 0f;

            var x = 0;
            var y = 0;
            var z = 0;

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
            float isoLevel = .5f;
            if (cubeDensity[0] < isoLevel) cubeindex |= 1;
            if (cubeDensity[1] < isoLevel) cubeindex |= 2;
            if (cubeDensity[2] < isoLevel) cubeindex |= 4;
            if (cubeDensity[3] < isoLevel) cubeindex |= 8;
            if (cubeDensity[4] < isoLevel) cubeindex |= 16;
            if (cubeDensity[5] < isoLevel) cubeindex |= 32;
            if (cubeDensity[6] < isoLevel) cubeindex |= 64;
            if (cubeDensity[7] < isoLevel) cubeindex |= 128;


            Debug.Log(cubeindex);

            var currentTriangulationIndex = cubeindex * 16;
            for (int i = currentTriangulationIndex; triTable[i] != -1; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    var a0 = cornerIndexAFromEdge[triTable[i + j]];
                    var b0 = cornerIndexBFromEdge[triTable[i + j]];
                    vertices.Add(Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0])));
                }

                triangles.Add(currentVertexIndex + 0);
                triangles.Add(currentVertexIndex + 1);
                triangles.Add(currentVertexIndex + 2);

                currentVertexIndex += 3;

            }
        }
    }

}
