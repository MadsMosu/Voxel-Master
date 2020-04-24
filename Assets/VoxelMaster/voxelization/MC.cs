using System.Collections.Generic;
using UnityEngine;

public class MC {

    public MeshData GenerateMesh (Voxel[] voxels, float isoLevel, Vector3Int size, Vector3 voxelScale) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();

        for (int x = 0; x < size.z - 1; x++)
            for (int y = 0; y < size.y - 1; y++)
                for (int z = 0; z < size.x - 1; z++) {
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
            cubeDensities[i] = voxels[Util.Map3DTo1D (cellPos + Tables.CornerIndex[i], size)].density;
            if (cubeDensities[i] < isoLevel) {
                caseCode |= addToCaseCode;
            }
            addToCaseCode *= 2;
        }

        if (caseCode == 0 || caseCode == 255) return;

        byte regularCellClass = Tables.RegularCellClass[caseCode];
        Tables.RegularCell regularCell = Tables.RegularCellData[regularCellClass];

        byte[] cellIndices = regularCell.GetIndices ();
        int[] indicesMapping = new int[cellIndices.Length]; //maps the added indices to the vertexData indices

        long vertexCount = regularCell.GetVertexCount ();
        long triangleCount = regularCell.GetTriangleCount ();
        for (int i = 0; i < vertexCount; i++) {
            byte edgeCode = (byte) (Tables.RegularVertexData[caseCode][i]);

            byte cornerA = (byte) ((edgeCode >> 4) & 0x0F);
            byte cornerB = (byte) (edgeCode & 0x0F);

            float densityA = cubeDensities[cornerA];
            float densityB = cubeDensities[cornerB];

            var p0Int = (cellPos + Tables.CornerIndex[cornerA]);
            var p0 = new Vector3 (p0Int.x * voxelScale.x, p0Int.y * voxelScale.y, p0Int.z * voxelScale.z);
            var p1Int = (cellPos + Tables.CornerIndex[cornerB]);
            var p1 = new Vector3 (p1Int.x * voxelScale.x, p1Int.y * voxelScale.y, p1Int.z * voxelScale.z);

            // Vector3 cornerANormal = GetRegularCornerNormal (voxels, p0Int, size, step);
            // Vector3 cornerBNormal = GetRegularCornerNormal (voxels, p1Int, size, step);

            // if (step > 1) {
            //     p0 = ShiftRegularCornerPos (p0Int, p0, size, step);
            //     p1 = ShiftRegularCornerPos (p1Int, p1, size, step);
            // }

            float lerpFactor;
            if (Mathf.Abs (densityB - densityA) > 0.000001f) {
                lerpFactor = (isoLevel - densityA) / (densityB - densityA);
            } else {
                lerpFactor = 0.5f;
            }
            var Q = p0 + lerpFactor * (p1 - p0);

            vertices.Add (Q);
            indicesMapping[i] = vertices.Count - 1;
            // normals.Add (GetVertexNormal (cornerANormal, cornerBNormal, lerpFactor));
        }

        for (int t = 0; t < triangleCount * 3; t += 3) {
            int vertexIndex0 = indicesMapping[cellIndices[t]];
            int vertexIndex1 = indicesMapping[cellIndices[t + 1]];
            int vertexIndex2 = indicesMapping[cellIndices[t + 2]];

            Vector3 vertex0 = vertices[vertexIndex0];
            Vector3 vertex1 = vertices[vertexIndex1];
            Vector3 vertex2 = vertices[vertexIndex2];
            if (vertex0 == vertex1 || vertex0 == vertex2 || vertex1 == vertex2) continue; //triangle with zero space

            triangleIndices.Add (vertexIndex0);
            triangleIndices.Add (vertexIndex2);
            triangleIndices.Add (vertexIndex1);
        }
    }
}