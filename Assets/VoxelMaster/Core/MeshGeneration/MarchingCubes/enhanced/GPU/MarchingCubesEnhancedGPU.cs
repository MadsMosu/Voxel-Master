using UnityEngine;

public class MarchingCubesEnhancedGPU {
    ComputeShader MCEnhancedCompute;
    private int kernelRegular, kernelTrans;
    ComputeBuffer regularCellVoxelsBuffer, transCellVoxelsBuffer, verticesBuffer, normalsBuffer, indicesBuffer;
    ComputeBuffer indicesCountBuffer;
    private int chunkSize, chunkSizeWithEdge, maxVertices, regularCellVoxelsLength, transCellVoxelsLength;
    private float isoLevel, scale;

    public MarchingCubesEnhancedGPU (MeshGeneratorSettings settings) {
        MCEnhancedCompute = (ComputeShader) Resources.Load ("MarchingCubesEnhanced");
        kernelRegular = MCEnhancedCompute.FindKernel ("PolygoniseRegularCell");
        kernelTrans = MCEnhancedCompute.FindKernel ("PolygoniseTransitionCell");

        this.chunkSize = settings.chunkSize;
        this.scale = settings.voxelScale;
        this.chunkSizeWithEdge = chunkSize + 3;
        this.isoLevel = settings.isoLevel;
        this.maxVertices = chunkSize * chunkSize * chunkSize * 12;
        this.regularCellVoxelsLength = chunkSizeWithEdge * chunkSizeWithEdge * chunkSizeWithEdge;
        this.transCellVoxelsLength = 6 * (2 * chunkSize + 3) * (2 * chunkSize + 3) * 3;
    }

    public MeshData GenerateMesh (IVoxelData volume, Vector3Int origin, int step) {
        verticesBuffer = new ComputeBuffer (maxVertices, sizeof (float) * 3, ComputeBufferType.Counter);
        normalsBuffer = new ComputeBuffer (maxVertices, sizeof (float) * 3, ComputeBufferType.Structured);
        indicesBuffer = new ComputeBuffer (maxVertices * 3, sizeof (int), ComputeBufferType.Append);
        regularCellVoxelsBuffer = new ComputeBuffer (regularCellVoxelsLength, sizeof (float) + sizeof (int), ComputeBufferType.Structured);
        transCellVoxelsBuffer = new ComputeBuffer (transCellVoxelsLength, sizeof (float) + sizeof (int), ComputeBufferType.Structured);
        indicesCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);

        // verticesBuffer.SetCounterValue (0);
        // normalsBuffer.SetCounterValue (0);
        indicesBuffer.SetCounterValue (0);
        // regularCellVoxelsBuffer.SetCounterValue (0);
        // transCellVoxelsBuffer.SetCounterValue (0);

        Voxel[] regularCellVoxels = new Voxel[regularCellVoxelsLength];
        Voxel[] transitionCellVoxels = new Voxel[transCellVoxelsLength];
        FillRegularCellVoxels (volume, ref regularCellVoxels, origin, step);
        FillTransCellVoxels (volume, origin, ref transitionCellVoxels, regularCellVoxels, step);

        MCEnhancedCompute.SetFloat ("isoLevel", isoLevel);
        MCEnhancedCompute.SetFloat ("scale", scale);
        MCEnhancedCompute.SetInt ("chunkSize", chunkSize);
        MCEnhancedCompute.SetInt ("step", step);

        regularCellVoxelsBuffer.SetData (regularCellVoxels);
        transCellVoxelsBuffer.SetData (transitionCellVoxels);

        MCEnhancedCompute.SetBuffer (kernelRegular, "VerticesBuffer", verticesBuffer);
        MCEnhancedCompute.SetBuffer (kernelRegular, "NormalsBuffer", normalsBuffer);
        MCEnhancedCompute.SetBuffer (kernelRegular, "IndicesBuffer", indicesBuffer);
        MCEnhancedCompute.SetBuffer (kernelRegular, "RegularCellVoxelsBuffer", regularCellVoxelsBuffer);
        MCEnhancedCompute.SetBuffer (kernelRegular, "TransCellVoxelsBuffer", transCellVoxelsBuffer);

        MCEnhancedCompute.Dispatch (kernelRegular, chunkSize / 8, chunkSize / 8, chunkSize / 8);

        ComputeBuffer.CopyCount (indicesBuffer, indicesCountBuffer, 0);
        int[] indicesCount = { 0 };
        indicesCountBuffer.GetData (indicesCount);
        int numIndices = indicesCount[0] * 3;
        Debug.Log (numIndices);

        if (numIndices == 0) {
            ReleaseBuffers ();
            return new MeshData (new Vector3[0], new int[0], new Vector3[0]);
        }

        // MCEnhancedCompute.SetBuffer (kernelTrans, "VerticesBuffer", verticesBuffer);
        // MCEnhancedCompute.SetBuffer (kernelTrans, "NormalsBuffer", normalsBuffer);
        // MCEnhancedCompute.SetBuffer (kernelTrans, "IndicesBuffer", indicesBuffer);
        // MCEnhancedCompute.SetBuffer (kernelTrans, "ReguarCellVoxelsBuffer", regularCellVoxelsBuffer);
        // MCEnhancedCompute.SetBuffer (kernelTrans, "TransCellVoxelsBuffer", transCellVoxelsBuffer);

        // MCEnhancedCompute.Dispatch (kernelTrans, 6, chunkSize / 8, chunkSize / 8);

        Vector3[] vertices = new Vector3[maxVertices];
        Vector3[] normals = new Vector3[maxVertices];
        int[] indices = new int[numIndices];
        verticesBuffer.GetData (vertices);
        normalsBuffer.GetData (normals);
        indicesBuffer.GetData (indices);

        ReleaseBuffers ();
        return new MeshData (vertices, indices, normals);
    }

    private void FillRegularCellVoxels (IVoxelData volume, ref Voxel[] regularCellVoxels, Vector3Int origin, int step) {
        for (int x = 0; x <= chunkSize + 2; x++)
            for (int y = 0; y <= chunkSize + 2; y++)
                for (int z = 0; z <= chunkSize + 2; z++) {
                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    Vector3Int coords = origin + (cellPos - Vector3Int.one) * step;
                    int index = x + chunkSizeWithEdge * y + chunkSizeWithEdge * chunkSizeWithEdge * z;
                    regularCellVoxels[index] = volume[coords];
                }
    }

    private void FillTransCellVoxels (IVoxelData volume, Vector3Int origin, ref Voxel[] transitionCellVoxels, Voxel[] regularCellVoxels, int step) {
        for (int side = 0; side < 6; side++) {
            for (int u = 0; u <= 2 * chunkSize + 2; u++)
                for (int v = 0; v <= 2 * chunkSize + 2; v++)
                    for (int w = 0; w <= 2; w++) {
                        int index = side + 6 * u + 6 * (2 * chunkSize + 3) * v + 6 * (2 * chunkSize + 3) * (2 * chunkSize + 3) * w;
                        if (w == 1 && u % 2 == 1 && v % 2 == 1) {
                            Vector3Int coords = new Vector3Int (
                                Tables.transFullFaceOrientation[side][0].x * chunkSize + ((u - 1) / 2) * Tables.transFullFaceOrientation[side][1].x + ((v - 1) / 2) * Tables.transFullFaceOrientation[side][2].x,
                                Tables.transFullFaceOrientation[side][0].y * chunkSize + ((u - 1) / 2) * Tables.transFullFaceOrientation[side][1].y + ((v - 1) / 2) * Tables.transFullFaceOrientation[side][2].y,
                                Tables.transFullFaceOrientation[side][0].z * chunkSize + ((u - 1) / 2) * Tables.transFullFaceOrientation[side][1].z + ((v - 1) / 2) * Tables.transFullFaceOrientation[side][2].z
                            );
                            transitionCellVoxels[index] = GetRegularCellVoxel (regularCellVoxels, coords);
                        } else {
                            Vector3 coords = new Vector3 (
                                origin.x + (Tables.transFullFaceOrientation[side][0].x * chunkSize + (u - 1) * 0.5f * Tables.transFullFaceOrientation[side][1].x + (v - 1) * 0.5f * Tables.transFullFaceOrientation[side][2].x + (w - 1) * 0.5f * Tables.transFullFaceOrientation[side][3].x) * step,
                                origin.y + (Tables.transFullFaceOrientation[side][0].y * chunkSize + (u - 1) * 0.5f * Tables.transFullFaceOrientation[side][1].y + (v - 1) * 0.5f * Tables.transFullFaceOrientation[side][2].y + (w - 1) * 0.5f * Tables.transFullFaceOrientation[side][3].y) * step,
                                origin.z + (Tables.transFullFaceOrientation[side][0].z * chunkSize + (u - 1) * 0.5f * Tables.transFullFaceOrientation[side][1].z + (v - 1) * 0.5f * Tables.transFullFaceOrientation[side][2].z + (w - 1) * 0.5f * Tables.transFullFaceOrientation[side][3].z) * step
                            );
                            transitionCellVoxels[index] = volume[coords];
                        }
                    }
        }
    }

    private Voxel GetRegularCellVoxel (Voxel[] regularCellVoxels, Vector3Int coords) {
        var index = (coords.x + 1) + chunkSizeWithEdge * (coords.y + 1) + chunkSizeWithEdge * chunkSizeWithEdge * (coords.z + 1);
        return regularCellVoxels[index];
    }

    private void ReleaseBuffers () {
        regularCellVoxelsBuffer?.Release ();
        transCellVoxelsBuffer?.Release ();
        verticesBuffer?.Release ();
        normalsBuffer?.Release ();
        indicesBuffer?.Release ();
        indicesCountBuffer?.Release ();
    }

}