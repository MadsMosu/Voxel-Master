// using System;
// using System.Collections.Generic;
// using UnityEngine;

// public class MarchingCubes : VoxelMeshGenerator {
//     private float isoLevel;
//     public override void Init (MeshGeneratorSettings settings) {
//         this.isoLevel = settings.isoLevel;
//     }

//     public override MeshData GenerateMesh (IVoxelData voxelData, Vector3Int origin, int size, int lod) {
//         int numCells = size * size * size;
//         List<Vector3> vertices = new List<Vector3> (5 * numCells * 3);
//         List<int> triangleIndices = new List<int> (5 * numCells * 3);

//         for (int x = 0; x < size - 1; x++)
//             for (int y = 0; y < size - 1; y++)
//                 for (int z = 0; z < size - 1; z++) {

//                     Vector3Int cellPos = new Vector3Int (x, y, z);
//                     PolygonizeCell (voxelData, origin, cellPos, ref vertices, ref triangleIndices, lod);

//                 }
//         Vector3[] surfaceNormals = CalculateSurfaceNormals (vertices.ToArray (), triangleIndices.ToArray ());
//         var normals = CalculateVertexNormals (vertices.ToArray (), surfaceNormals);

//         return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals);
//     }

//     internal void PolygonizeCell (IVoxelData volume, Vector3Int offsetPos, Vector3Int cellPos, ref List<Vector3> vertices, ref List<int> triangleIndices, int lod) {
//         offsetPos += cellPos;

//         float[] cubeDensities = new float[8];
//         byte caseCode = 0;
//         byte addToCaseCode = 1;
//         for (int i = 0; i < cubeDensities.Length; i++) {
//             cubeDensities[i] = volume[offsetPos + Lookup.cubeVertOffsets[i]].density;
//             if (cubeDensities[i] < isoLevel) {
//                 caseCode |= addToCaseCode;
//             }
//             addToCaseCode *= 2;
//         }

//         if (caseCode == 0 || caseCode == 255) return;
//         int triangleIndex = 0;

//         int[] triangulation = Lookup.triTable[caseCode];
//         for (int i = 0; triangulation[i] != -1; i += 3) {
//             for (int j = 0; j < 3; j++) {
//                 var a = Lookup.cornerIndexAFromEdge[triangulation[i + j]];
//                 var b = Lookup.cornerIndexBFromEdge[triangulation[i + j]];

//                 Vector3Int aPos = cellPos + Lookup.cubeVertOffsets[a];
//                 Vector3Int bPos = cellPos + Lookup.cubeVertOffsets[b];
//                 float lerp = (isoLevel - cubeDensities[a]) / (cubeDensities[b] - cubeDensities[a]);
//                 var vertex = Vector3.Lerp (aPos, bPos, lerp);

//                 vertices.Add (vertex);
//                 triangleIndices.Add (triangleIndex++);
//             }
//         }
//     }

//     private Vector3[] CalculateSurfaceNormals (Vector3[] vertices, int[] triangleIndices) {
//         Vector3[] surfaceNormals = new Vector3[vertices.Length];
//         for (int i = 0; i < vertices.Length / 3; i++) {
//             int index = i * 3;
//             int indexA = triangleIndices[index];
//             int indexB = triangleIndices[index + 1];
//             int indexC = triangleIndices[index + 2];

//             Vector3 surfaceNormal = Vector3.Normalize (SurfaceNormalFromIndices (vertices, indexA, indexB, indexC));
//             surfaceNormals[indexA] = surfaceNormal;
//             surfaceNormals[indexB] = surfaceNormal;
//             surfaceNormals[indexC] = surfaceNormal;
//         }
//         return surfaceNormals;
//     }

//     private Vector3 SurfaceNormalFromIndices (Vector3[] vertices, int indexA, int indexB, int indexC) {
//         Vector3 AB = vertices[indexB] - vertices[indexA];
//         Vector3 AC = vertices[indexC] - vertices[indexA];
//         return Vector3.Cross (AB, AC);
//     }

//     private Vector3[] CalculateVertexNormals (Vector3[] vertices, Vector3[] surfaceNormals) {
//         Vector3[] vertexNormals = new Vector3[surfaceNormals.Length];
//         Dictionary<int, Vector3> sums = new Dictionary<int, Vector3> ();
//         Dictionary<int, int> sharedVertices = new Dictionary<int, int> ();

//         for (int i = 0; i < vertices.Length; i++) {
//             if (sharedVertices.ContainsKey (i)) continue;

//             Vector3 pos = vertices[i];
//             Vector3 sum = surfaceNormals[i];
//             sums[i] = sum;
//             for (int j = i + 1; j < vertices.Length; j++) {
//                 if (pos == vertices[j]) {
//                     sums[i] += surfaceNormals[j];
//                     sharedVertices[j] = i;
//                 }
//             }
//             vertexNormals[i] = sums[i].normalized;
//         }

//         foreach (KeyValuePair<int, int> entry in sharedVertices) {
//             vertexNormals[entry.Key] = sums[entry.Value].normalized;
//         }
//         return vertexNormals;
//     }
// }