using System;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubes : VoxelMeshGenerator {
    public override MeshData GenerateMesh (VoxelChunk chunk) {
        int numCells = chunk.size * chunk.size * chunk.size;
        var vertices = new List<Vector3> (5 * numCells * 3);
        var triangleIndices = new List<int> (5 * numCells * 3);
        int triangleIndex = 0;

        chunk.voxels.Traverse (delegate (int x, int y, int z, Voxel v) {
            var cellPos = new Vector3Int (x, y, z);
            if (cellPos.x + 1 >= chunk.size || cellPos.y + 1 >= chunk.size || cellPos.z + 1 >= chunk.size) return;

            float[] cubeDensity = new float[8] {
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[0]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[1]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[2]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[3]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[4]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[5]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[6]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[7]).density,
            };

            int cubeindex = 0;
            if (cubeDensity[0] < chunk.isoLevel) cubeindex |= 1;
            if (cubeDensity[1] < chunk.isoLevel) cubeindex |= 2;
            if (cubeDensity[2] < chunk.isoLevel) cubeindex |= 4;
            if (cubeDensity[3] < chunk.isoLevel) cubeindex |= 8;
            if (cubeDensity[4] < chunk.isoLevel) cubeindex |= 16;
            if (cubeDensity[5] < chunk.isoLevel) cubeindex |= 32;
            if (cubeDensity[6] < chunk.isoLevel) cubeindex |= 64;
            if (cubeDensity[7] < chunk.isoLevel) cubeindex |= 128;

            int[] triangulation = Lookup.triTable[cubeindex];
            for (int i = 0; triangulation[i] != -1; i += 3) {
                for (int j = 0; j < 3; j++) {
                    var a = Lookup.cornerIndexAFromEdge[triangulation[i + j]];
                    var b = Lookup.cornerIndexBFromEdge[triangulation[i + j]];

                    Vector3Int aPos = cellPos + Lookup.cubeVertOffsets[a];
                    Vector3Int bPos = cellPos + Lookup.cubeVertOffsets[b];
                    float lerp = (chunk.isoLevel - cubeDensity[a]) / (cubeDensity[b] - cubeDensity[a]);
                    var vertex = Vector3.Lerp (aPos, bPos, lerp);

                    vertices.Add (vertex);
                    triangleIndices.Add (triangleIndex++);
                }
            }
        });
        Vector3[] surfaceNormals = CalculateSurfaceNormals (vertices.ToArray (), triangleIndices.ToArray ());
        var normals = CalculateVertexNormals (vertices.ToArray (), surfaceNormals);

        return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals);
    }

    private Vector3[] CalculateSurfaceNormals (Vector3[] vertices, int[] triangleIndices) {
        Vector3[] surfaceNormals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length / 3; i++) {
            int index = i * 3;
            int indexA = triangleIndices[index];
            int indexB = triangleIndices[index + 1];
            int indexC = triangleIndices[index + 2];

            Vector3 surfaceNormal = Vector3.Normalize (SurfaceNormalFromIndices (vertices, indexA, indexB, indexC));
            surfaceNormals[indexA] = surfaceNormal;
            surfaceNormals[indexB] = surfaceNormal;
            surfaceNormals[indexC] = surfaceNormal;
        }
        return surfaceNormals;
    }

    private Vector3 SurfaceNormalFromIndices (Vector3[] vertices, int indexA, int indexB, int indexC) {
        Vector3 AB = vertices[indexB] - vertices[indexA];
        Vector3 AC = vertices[indexC] - vertices[indexA];
        return Vector3.Cross (AB, AC);
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

    public override void Init (MeshGeneratorSettings settings) {
        throw new NotImplementedException ();
    }

}