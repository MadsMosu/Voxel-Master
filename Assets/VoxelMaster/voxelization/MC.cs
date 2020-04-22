using System.Collections.Generic;
using UnityEngine;

public class MC {

    public MeshData GenerateMesh (Voxel[] voxels, float isoLevel, Vector3Int size, Vector3 voxelScale) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();

        for (int x = 0; x < size.x - 1; x++)
            for (int y = 0; y < size.y - 1; y++)
                for (int z = 0; z < size.z - 1; z++) {
                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (voxels, cellPos, ref vertices, ref triangleIndices, isoLevel, size, voxelScale);
                }

        return new MeshData (vertices.ToArray (), triangleIndices.ToArray ());
    }

    internal void PolygonizeCell (Voxel[] voxels, Vector3Int cellPos, ref List<Vector3> vertices, ref List<int> triangleIndices, float isoLevel, Vector3Int size, Vector3 voxelScale) {

        float[] cubeDensities = new float[8];
        byte caseCode = 0;
        byte addToCaseCode = 1;
        for (int i = 0; i < cubeDensities.Length; i++) {
            cubeDensities[i] = voxels[Util.Map3DTo1D (cellPos + Lookup.cubeVertOffsets[i], size)].density;
            if (cubeDensities[i] < isoLevel) {
                caseCode |= addToCaseCode;
            }
            addToCaseCode *= 2;
        }

        if (caseCode == 0 || caseCode == 255) return;
        int triangleIndex = 0;

        int[] triangulation = Lookup.triTable[caseCode];
        for (int i = 0; triangulation[i] != -1; i += 3) {
            for (int j = 0; j < 3; j++) {
                var a = Lookup.cornerIndexAFromEdge[triangulation[i + j]];
                var b = Lookup.cornerIndexBFromEdge[triangulation[i + j]];

                Vector3Int aPos = cellPos + Lookup.cubeVertOffsets[a];
                Vector3Int bPos = cellPos + Lookup.cubeVertOffsets[b];
                float lerp = (isoLevel - cubeDensities[a]) / (cubeDensities[b] - cubeDensities[a]);
                var vertex = Vector3.Lerp (aPos, bPos, lerp);

                vertices.Add (new Vector3 (vertex.x * voxelScale.x, vertex.y * voxelScale.y, vertex.z * voxelScale.z));
                triangleIndices.Add (triangleIndex++);
            }
        }
    }
}