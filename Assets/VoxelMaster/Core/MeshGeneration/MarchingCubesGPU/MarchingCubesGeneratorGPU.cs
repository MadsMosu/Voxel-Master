using System.Collections.Generic;
using UnityEngine;
using VoxelMaster.Chunk;

public class MarchingCubesGPU {
    private ComputeShader marchingCubesCompute;
    private ComputeBuffer trianglesBuffer, triangleCountBuffer, densitiesBuffer, verticesBuffer, surfaceNormalsBuffer, triangleIndicesBuffer;
    private int kernelMC, indicesAndNormalKernel;

    private int maxTriangles, numCells, numVoxels;

    public MarchingCubesGPU () {
        marchingCubesCompute = (ComputeShader) Resources.Load ("MarchingCubesGPU");
        kernelMC = marchingCubesCompute.FindKernel ("MarchingCubes");
        indicesAndNormalKernel = marchingCubesCompute.FindKernel ("GenerateTriangleIndicesAndNormals");
    }

    int isoLevel = 0;
    public MeshData GenerateMesh (VoxelChunk chunk) {
        numCells = chunk.size * chunk.size * chunk.size;
        numVoxels = (chunk.size - 1) * (chunk.size - 1) * (chunk.size - 1);
        maxTriangles = numVoxels * 5;

        trianglesBuffer = new ComputeBuffer (maxTriangles, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triangleCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
        densitiesBuffer = new ComputeBuffer (numCells, sizeof (float) + sizeof (int), ComputeBufferType.Structured);
        verticesBuffer = new ComputeBuffer (maxTriangles * 3, sizeof (float) * 3, ComputeBufferType.Structured);
        surfaceNormalsBuffer = new ComputeBuffer (maxTriangles * 3, sizeof (float) * 3, ComputeBufferType.Structured);
        triangleIndicesBuffer = new ComputeBuffer (maxTriangles * 3, sizeof (int), ComputeBufferType.Structured);

        trianglesBuffer.SetCounterValue (0);
        marchingCubesCompute.SetFloat ("isoLevel", isoLevel);
        marchingCubesCompute.SetInt ("chunkSize", chunk.size);

        densitiesBuffer.SetData (chunk.voxels.ToArray ());

        marchingCubesCompute.SetBuffer (kernelMC, "DensitiesBuffer", densitiesBuffer);
        marchingCubesCompute.SetBuffer (kernelMC, "TrianglesBuffer", trianglesBuffer);

        marchingCubesCompute.Dispatch (kernelMC, chunk.size / 8, chunk.size / 8, chunk.size / 8);

        ComputeBuffer.CopyCount (trianglesBuffer, triangleCountBuffer, 0);
        int[] triCount = { 0 };
        triangleCountBuffer.GetData (triCount);
        int numTriangles = triCount[0];

        if (numTriangles == 0) {
            ReleaseBuffers ();
            return new MeshData (new Vector3[0], new int[0], new Vector3[0]);
        }

        Vector3[] vertices = new Vector3[numTriangles * 3];
        trianglesBuffer.GetData (vertices);
        verticesBuffer.SetData (vertices);

        //Calculate indices and surface normals for all triangles
        marchingCubesCompute.SetBuffer (indicesAndNormalKernel, "TriangleIndicesBuffer", triangleIndicesBuffer);
        marchingCubesCompute.SetBuffer (indicesAndNormalKernel, "VerticesBuffer", verticesBuffer);
        marchingCubesCompute.SetBuffer (indicesAndNormalKernel, "SurfaceNormalsBuffer", surfaceNormalsBuffer);

        marchingCubesCompute.Dispatch (indicesAndNormalKernel, numTriangles, 1, 1);

        int[] triangleIndices = new int[vertices.Length];
        triangleIndicesBuffer.GetData (triangleIndices);

        Vector3[] surfaceNormals = new Vector3[vertices.Length];
        surfaceNormalsBuffer.GetData (surfaceNormals);

        //Vector3[] normals = CalculateVertexNormals(vertices, surfaceNormals);

        ReleaseBuffers ();

        return new MeshData (vertices, triangleIndices, surfaceNormals);
    }

    private Vector3[] CalculateVertexNormals (Vector3[] vertices, Vector3[] surfaceNormals) {
        Vector3[] vertexNormals = new Vector3[surfaceNormals.Length];
        Dictionary<int, Vector3> sums = new Dictionary<int, Vector3> ();
        Dictionary<int, int> sharedVertices = new Dictionary<int, int> ();

        for (int i = 0; i < vertices.Length; i++) {
            if (sharedVertices.ContainsKey (i)) continue;

            Vector3 pos = vertices[i];
            Vector3 sum = surfaceNormals[i];
            sums[i] = sum;
            for (int j = i + 1; j < vertices.Length; j++) {
                if (pos == vertices[j]) {
                    sums[i] += surfaceNormals[j];
                    sharedVertices[j] = i;
                }
            }
            vertexNormals[i] = sums[i].normalized;
        }

        foreach (KeyValuePair<int, int> entry in sharedVertices) {
            vertexNormals[entry.Key] = sums[entry.Value].normalized;
        }
        return vertexNormals;
    }

    private void ReleaseBuffers () {
        trianglesBuffer?.Release ();
        triangleCountBuffer?.Release ();
        densitiesBuffer?.Release ();
        verticesBuffer.Release ();
        surfaceNormalsBuffer?.Release ();
        triangleIndicesBuffer?.Release ();
    }
}