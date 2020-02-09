
using UnityEngine;

public class MarchingCubesGPU : VoxelMeshGenerator
{
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triangleIndiciesBuffer;
    private ComputeShader marchingCubesCompute;
    private int kernelMC;
    public override MeshData generateMesh(VoxelChunk chunk)
    {
        kernelMC = marchingCubesCompute.FindKernel("MarchingCubes");
        int numCells = chunk.size.x * chunk.size.y * chunk.size.z;
        triangleBuffer = new ComputeBuffer(numCells * 5, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        triangleIndiciesBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        marchingCubesCompute.SetFloat("isoLevel", chunk.isoLevel);
        marchingCubesCompute.SetBuffer(kernelMC, "trianglesRW", triangleBuffer);

        triangleBuffer.SetCounterValue(0);
        marchingCubesCompute.Dispatch(kernelMC, chunk.size.x / 8, chunk.size.y / 8, chunk.size.z / 8);

        ComputeBuffer.CopyCount(triangleBuffer, triangleIndiciesBuffer, 0);
        int[] triCount = new int[1];
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

        return new MeshData(vertices, triangleIndicies);
    }

    private struct Triangle
    {
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