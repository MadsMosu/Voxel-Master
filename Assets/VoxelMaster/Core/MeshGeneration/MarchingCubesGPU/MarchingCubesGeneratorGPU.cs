
using System;
using UnityEngine;

public class MarchingCubesGPU : VoxelMeshGenerator
{
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triangleIndiciesBuffer;
    private ComputeBuffer densitiesBuffer;
    private ComputeShader marchingCubesCompute;
    private int kernelMC;

    private int Map3DTo1D(Vector3Int coords, Vector3Int size)
    {
        return coords.x + size.y * (coords.y + size.z * coords.z);
    }

    public override MeshData generateMesh(VoxelChunk chunk, Func<Vector3, float> densityFunction)
    {
        marchingCubesCompute = (ComputeShader)Resources.Load("MarchingCubesGPU");
        kernelMC = marchingCubesCompute.FindKernel("MarchingCubes");

        int numCells = chunk.size.x * chunk.size.y * chunk.size.z;
        int numVoxels = (chunk.size.x - 1) * (chunk.size.y - 1) * (chunk.size.z - 1);
        int maxTriangles = numVoxels * 5;

        triangleBuffer = new ComputeBuffer(maxTriangles, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        triangleIndiciesBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        densitiesBuffer = new ComputeBuffer(numCells, sizeof(float), ComputeBufferType.Structured);

        triangleBuffer.SetCounterValue(0);
        marchingCubesCompute.SetFloat("isoLevel", chunk.isoLevel);
        marchingCubesCompute.SetInt("chunkLength", chunk.size.x);
        marchingCubesCompute.SetInt("chunkWidth", chunk.size.y);
        marchingCubesCompute.SetInt("chunkDepth", chunk.size.z);

        float[] densities = new float[numCells];
        for (int x = 0; x < chunk.size.x; x++)
            for (int y = 0; y < chunk.size.y; y++)
                for (int z = 0; z < chunk.size.z; z++)
                {
                    densities[Map3DTo1D(new Vector3Int(x, y, z), chunk.size)] = chunk.voxels.GetVoxel(new Vector3Int(x, y, z)).density;
                }

        densitiesBuffer.SetData(densities);
        marchingCubesCompute.SetBuffer(kernelMC, "densities", densitiesBuffer);
        marchingCubesCompute.SetBuffer(kernelMC, "triangles", triangleBuffer);

        marchingCubesCompute.Dispatch(kernelMC, chunk.size.x / 8, chunk.size.y / 8, chunk.size.z / 8);

        ComputeBuffer.CopyCount(triangleBuffer, triangleIndiciesBuffer, 0);
        int[] triCount = { 0 };
        triangleIndiciesBuffer.GetData(triCount);
        int numTriangles = triCount[0];

        Triangle[] triangles = new Triangle[numTriangles];
        triangleBuffer.GetData(triangles, 0, 0, numTriangles);

        Vector3[] vertices = new Vector3[numTriangles * 3];
        int[] triangleIndicies = new int[numTriangles * 3];

        for (int i = 0; i < numTriangles; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                triangleIndicies[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = triangles[i][j];
            }
        }

        densitiesBuffer.Release();
        triangleBuffer.Release();
        triangleIndiciesBuffer.Release();
        return new MeshData(vertices, triangleIndicies);
    }

    private struct Triangle
    {
#pragma warning disable 649
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
}