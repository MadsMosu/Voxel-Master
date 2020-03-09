using System;
using UnityEngine;
using UnityEngine.Rendering;

public class DualContouringGPU : VoxelMeshGenerator {

    private ComputeShader dualContouringCompute, normalsShader;
    private ComputeBuffer densitiesBuffer, indicesBuffer, verticesBuffer, cellVerticesBuffer, quadsBuffer, quadCountBuffer, cellVerticesCountBuffer;

    private int cellVerticesKernel, connectVerticesKernel, indicesKernel, normalsMainKernel;

    private RenderTexture densityTexture;

    public DualContouringGPU () {
        dualContouringCompute = (ComputeShader) Resources.Load ("DualContouringGPU");
        normalsShader = (ComputeShader) Resources.Load ("Normals");

        normalsMainKernel = normalsShader.FindKernel ("CSMain");

        cellVerticesKernel = dualContouringCompute.FindKernel ("GenerateCellVertices");
        connectVerticesKernel = dualContouringCompute.FindKernel ("ConnectCellVertices");
        indicesKernel = dualContouringCompute.FindKernel ("GenerateTriangleIndices");

    }
    public override MeshData GenerateMesh (VoxelChunk chunk, Func<Vector3, float> densityFunction) {
        int numCells = chunk.size.x * chunk.size.y * chunk.size.z;

        densitiesBuffer = new ComputeBuffer (numCells, sizeof (float), ComputeBufferType.Structured);
        verticesBuffer = new ComputeBuffer (numCells, sizeof (float) * 3 * 2, ComputeBufferType.Structured);
        cellVerticesBuffer = new ComputeBuffer (numCells, sizeof (float) * 3 * 2, ComputeBufferType.Append);
        quadsBuffer = new ComputeBuffer (numCells, sizeof (float) * 3 * 8, ComputeBufferType.Append);

        quadCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
        cellVerticesCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);

        densityTexture = new RenderTexture (chunk.size.x, chunk.size.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        densityTexture.dimension = TextureDimension.Tex3D;
        densityTexture.enableRandomWrite = true;
        densityTexture.useMipMap = false;
        densityTexture.volumeDepth = chunk.size.z;
        densityTexture.Create ();

        //Get all voxel densities and parse it to the densities buffer
        float[] densities = new float[numCells];
        chunk.voxels.Traverse (delegate (int x, int y, int z, Voxel v) {
            densities[Util.Map3DTo1D (new Vector3Int (x, y, z), chunk.size)] = v.density;
        });
        densitiesBuffer.SetData (densities);

        //Create the normals for all chunk voxels
        normalsShader.SetInt ("chunkWidth", chunk.size.x);
        normalsShader.SetInt ("chunkHeight", chunk.size.y);
        normalsShader.SetInt ("chunkDepth", chunk.size.z);
        normalsShader.SetBuffer (normalsMainKernel, "DensitiesBuffer", densitiesBuffer);
        normalsShader.SetTexture (normalsMainKernel, "DensityTexture", densityTexture);

        normalsShader.Dispatch (0, chunk.size.x / 8, chunk.size.y / 8, chunk.size.z / 8);

        cellVerticesBuffer.SetCounterValue (0);
        quadsBuffer.SetCounterValue (0);

        dualContouringCompute.SetInt ("chunkWidth", chunk.size.x);
        dualContouringCompute.SetInt ("chunkHeight", chunk.size.y);
        dualContouringCompute.SetInt ("chunkDepth", chunk.size.z);
        dualContouringCompute.SetFloat ("isoLevel", chunk.isoLevel);

        //Finds all edge intersections
        dualContouringCompute.SetTexture (cellVerticesKernel, "DensityTexture", densityTexture);
        dualContouringCompute.SetBuffer (cellVerticesKernel, "DensitiesBuffer", densitiesBuffer);
        dualContouringCompute.SetBuffer (cellVerticesKernel, "CellVerticesBuffer", cellVerticesBuffer);
        dualContouringCompute.Dispatch (cellVerticesKernel, chunk.size.x / 8, chunk.size.y / 8, chunk.size.z / 8);

        //Get number of cell vertices
        ComputeBuffer.CopyCount (cellVerticesBuffer, cellVerticesCountBuffer, 0);
        int[] cellVerticesCount = { 0 };
        cellVerticesCountBuffer.GetData (cellVerticesCount);

        //if no cell vertices then return
        if (cellVerticesCount[0] == 0) {
            ReleaseBuffers ();
            return new MeshData (new Vector3[0], new int[0], new Vector3[0]);
        }

        Vertex[] cellVertices = new Vertex[cellVerticesCount[0]];
        cellVerticesBuffer.GetData (cellVertices);
        verticesBuffer.SetData (cellVertices);

        dualContouringCompute.SetBuffer (connectVerticesKernel, "VerticesBuffer", verticesBuffer);
        dualContouringCompute.SetBuffer (connectVerticesKernel, "DensitiesBuffer", densitiesBuffer);
        dualContouringCompute.SetBuffer (connectVerticesKernel, "QuadsBuffer", quadsBuffer);

        dualContouringCompute.Dispatch (connectVerticesKernel, chunk.size.x / 8, chunk.size.y / 8, chunk.size.z / 8);

        //Get count of quads generated
        ComputeBuffer.CopyCount (quadsBuffer, quadCountBuffer, 0);
        int[] quadCount = { 0 };
        quadCountBuffer.GetData (quadCount);

        Quad[] quads = new Quad[quadCount[0]];
        quadsBuffer.GetData (quads);

        Vector3[] vertices = new Vector3[quadCount[0] * 4];
        Vector3[] normals = new Vector3[quadCount[0] * 4];

        //Populate vertices and normals from quad array
        for (int i = 0; i < quadCount[0]; i++) {
            Quad quad = quads[i];
            for (int j = 0; j < 4; j++) {
                vertices[i * 4 + j] = quad[j].position;
                normals[i * 4 + j] = quad[j].normal;
            }
        }

        indicesBuffer = new ComputeBuffer (quadCount[0] * 4, sizeof (int) * 6, ComputeBufferType.Structured);
        dualContouringCompute.SetBuffer (indicesKernel, "IndicesBuffer", indicesBuffer);

        dualContouringCompute.Dispatch (indicesKernel, quadCount[0], 1, 1);

        //Get all triangle indices 
        int[] triangleIndices = new int[quadCount[0] * 6];
        indicesBuffer.GetData (triangleIndices);

        ReleaseBuffers ();

        return new MeshData (vertices, triangleIndices, normals);
    }

    private void ReleaseBuffers () {
        densitiesBuffer?.Release ();
        cellVerticesBuffer?.Release ();
        indicesBuffer?.Release ();
        quadsBuffer?.Release ();
        quadCountBuffer?.Release ();
        cellVerticesCountBuffer?.Release ();
        verticesBuffer?.Release ();
        densityTexture?.Release ();
    }

    private struct Quad {
#pragma warning disable 649
        public Vector3 vertex0, vertex1, vertex2, vertex3;
        public Vector3 normal0, normal1, normal2, normal3;

        public Vertex this [int i] {
            get {
                switch (i) {
                    case 0:
                        return new Vertex { position = vertex0, normal = normal0 };
                    case 1:
                        return new Vertex { position = vertex1, normal = normal1 };
                    case 2:
                        return new Vertex { position = vertex2, normal = normal2 };
                    default:
                        return new Vertex { position = vertex3, normal = normal3 };
                }
            }
        }
    }

    private struct Vertex {
#pragma warning disable 649
        public Vector3 position, normal;
    }
}