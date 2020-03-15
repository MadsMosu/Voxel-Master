using System;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubes : VoxelMeshGenerator {
    public override MeshData GenerateMesh (IVoxelData voxelData, Vector3Int origin, int size) {

        int numCells = size * size * size;
        var vertices = new List<Vector3> (5 * numCells * 3);
        var triangleIndices = new List<int> (5 * numCells * 3);
        int triangleIndex = 0;

        for (int z = 0; z < size; z++)
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) {
                    var cellPos = new Vector3Int (x, y, z);
                    if (cellPos.x + 1 >= size || cellPos.y + 1 >= size || cellPos.z + 1 >= size) continue;

                    sbyte[] cubeDensity = new sbyte[8] {
                        voxelData[cellPos + Lookup.cubeVertOffsets[0]].density,
                        voxelData[cellPos + Lookup.cubeVertOffsets[1]].density,
                        voxelData[cellPos + Lookup.cubeVertOffsets[2]].density,
                        voxelData[cellPos + Lookup.cubeVertOffsets[3]].density,
                        voxelData[cellPos + Lookup.cubeVertOffsets[4]].density,
                        voxelData[cellPos + Lookup.cubeVertOffsets[5]].density,
                        voxelData[cellPos + Lookup.cubeVertOffsets[6]].density,
                        voxelData[cellPos + Lookup.cubeVertOffsets[7]].density,
                    };

                    int cubeindex = 0;
                    if (cubeDensity[0] < 0) cubeindex |= 1;
                    if (cubeDensity[1] < 0) cubeindex |= 2;
                    if (cubeDensity[2] < 0) cubeindex |= 4;
                    if (cubeDensity[3] < 0) cubeindex |= 8;
                    if (cubeDensity[4] < 0) cubeindex |= 16;
                    if (cubeDensity[5] < 0) cubeindex |= 32;
                    if (cubeDensity[6] < 0) cubeindex |= 64;
                    if (cubeDensity[7] < 0) cubeindex |= 128;

                    int[] triangulation = Lookup.triTable[cubeindex];
                    for (int i = 0; triangulation[i] != -1; i += 3) {
                        for (int j = 0; j < 3; j++) {
                            var a = Lookup.cornerIndexAFromEdge[triangulation[i + j]];
                            var b = Lookup.cornerIndexBFromEdge[triangulation[i + j]];

                            Vector3Int aPos = cellPos + Lookup.cubeVertOffsets[a];
                            Vector3Int bPos = cellPos + Lookup.cubeVertOffsets[b];
                            float lerp = (0 - cubeDensity[a]) / (cubeDensity[b] - cubeDensity[a]);
                            var vertex = Vector3.Lerp (aPos, bPos, lerp);

                            vertices.Add (vertex);
                            triangleIndices.Add (triangleIndex++);
                        }
                    }
                }
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
        // throw new NotImplementedException ();
    }

}