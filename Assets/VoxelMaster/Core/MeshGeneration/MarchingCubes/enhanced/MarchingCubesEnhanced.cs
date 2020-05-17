using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced {
    private float isoLevel = 0f;
    private int chunkSize;
    private static float shiftFactor = 0.30f;

    public MeshData GenerateMesh (IVoxelData volume, Vector3 origin, int step, float scale, int chunkSize) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();
        List<Vector3> normals = new List<Vector3> ();

        for (int z = 0; z < chunkSize; z++)
            for (int y = 0; y < chunkSize; y++)
                for (int x = 0; x < chunkSize; x++) {
                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (volume, origin, cellPos, step, scale, ref vertices, ref triangleIndices, ref normals);
                }

        if (step > 1) {
            for (int side = 0; side < 6; side++)
                for (int u = 0; u < chunkSize; u++)
                    for (int v = 0; v < chunkSize; v++) {
                        Vector3Int cellPos = new Vector3Int (
                            Tables.transFullFaceOrientation[side][0].x * (chunkSize - 1) + u * Tables.transFullFaceOrientation[side][1].x + v * Tables.transFullFaceOrientation[side][2].x,
                            Tables.transFullFaceOrientation[side][0].y * (chunkSize - 1) + u * Tables.transFullFaceOrientation[side][1].y + v * Tables.transFullFaceOrientation[side][2].y,
                            Tables.transFullFaceOrientation[side][0].z * (chunkSize - 1) + u * Tables.transFullFaceOrientation[side][1].z + v * Tables.transFullFaceOrientation[side][2].z
                        );
                        PolygonizeTransitionCell (volume, origin, cellPos, scale, ref vertices, ref triangleIndices, ref normals, u, v, side, step);
                    }
        }

        return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals.ToArray ());
    }

    internal void PolygonizeCell (IVoxelData volume, Vector3 origin, Vector3Int cellPos, int step, float scale, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals) {

        float[] cubeDensities = new float[8];
        byte caseCode = 0;
        byte addToCaseCode = 1;
        for (int i = 0; i < cubeDensities.Length; i++) {
            cubeDensities[i] = volume[origin + ((cellPos + Tables.CornerIndex[i])) * step].density;
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
            var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z) * step;
            var p1Int = (cellPos + Tables.CornerIndex[cornerB]);
            var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z) * step;

            Vector3 cornerANormal = GetRegularCornerNormal (volume, origin, p0Int, step);
            Vector3 cornerBNormal = GetRegularCornerNormal (volume, origin, p1Int, step);

            if (step > 1) {
                p0 = ShiftRegularCornerPos (p0Int, p0, step);
                p1 = ShiftRegularCornerPos (p1Int, p1, step);
            }

            float lerpFactor;
            if (Mathf.Abs (densityB - densityA) > 0.000001f) {
                lerpFactor = (isoLevel - densityA) / (densityB - densityA);
            } else {
                lerpFactor = 0.5f;
            }
            var Q = p0 + lerpFactor * (p1 - p0);

            vertices.Add (Q * scale);
            indicesMapping[i] = vertices.Count - 1;
            normals.Add (GetVertexNormal (cornerANormal, cornerBNormal, lerpFactor));
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

    internal void PolygonizeTransitionCell (IVoxelData volume, Vector3 origin, Vector3Int cellPos, float scale, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int u, int v, int side, int step) {
        float[] transFullFaceDensities = new float[9];

        int[] caseCodeCoeffs = new int[9] { 0x01, 0x02, 0x04, 0x80, 0x100, 0x08, 0x40, 0x20, 0x10 };
        int transCaseCode = 0;
        for (byte i = 0; i < 9; i++) {
            transFullFaceDensities[i] = GetTransCornerDensity (volume, origin, cellPos, i, side, step);
            if (transFullFaceDensities[i] < isoLevel) {
                transCaseCode |= caseCodeCoeffs[i];
            }
        }

        if (transCaseCode == 0 || transCaseCode == 511) return;

        byte transCellClass = Tables.TransitionCellClass[transCaseCode];
        Tables.TransitionCell transCell = Tables.TransitionCellData[transCellClass & 0x7F];

        byte[] cellIndices = transCell.GetIndices ();
        int[] indicesMapping = new int[cellIndices.Length];
        bool flipWinding = (transCellClass & 0x80) == 0;

        long vertexCount = transCell.GetVertexCount ();
        long triangleCount = transCell.GetTriangleCount ();

        for (int i = 0; i < vertexCount; i++) {
            byte edgeCode = (byte) (Tables.TransitionVertexData[transCaseCode][i]);

            byte cornerA = (byte) ((edgeCode >> 4) & 0x0F);
            byte cornerB = (byte) (edgeCode & 0x0F);

            Vector3 p0 = GetTransCornerPos (cellPos, cornerA, side, step);
            Vector3 p1 = GetTransCornerPos (cellPos, cornerB, side, step);

            Vector3 cornerANormal = GetTransCornerNormal (volume, origin, cellPos, side, cornerA, step);
            Vector3 cornerBNormal = GetTransCornerNormal (volume, origin, cellPos, side, cornerB, step);

            float densityA = cornerA < 9 ? transFullFaceDensities[cornerA] : GetTransCornerDensity (volume, origin, cellPos, cornerA, side, step);
            float densityB = cornerB < 9 ? transFullFaceDensities[cornerB] : GetTransCornerDensity (volume, origin, cellPos, cornerB, side, step);

            float lerpFactor;
            if (Mathf.Abs (densityB - densityA) > 0.000001f) {
                lerpFactor = (isoLevel - densityA) / (densityB - densityA);
            } else {
                lerpFactor = 0.5f;
            }

            var Q = p0 + lerpFactor * (p1 - p0);

            vertices.Add (Q * scale);
            indicesMapping[i] = vertices.Count - 1;
            normals.Add (GetVertexNormal (cornerANormal, cornerBNormal, lerpFactor));
        }

        for (int t = 0; t < triangleCount * 3; t += 3) {
            int vertexIndex0 = indicesMapping[cellIndices[t]];
            int vertexIndex1 = indicesMapping[cellIndices[t + 1]];
            int vertexIndex2 = indicesMapping[cellIndices[t + 2]];

            Vector3 vertex0 = vertices[vertexIndex0];
            Vector3 vertex1 = vertices[vertexIndex1];
            Vector3 vertex2 = vertices[vertexIndex2];
            if (vertex0 == vertex1 || vertex0 == vertex2 || vertex1 == vertex2) continue; //triangle with zero space

            if (flipWinding) {
                triangleIndices.Add (vertexIndex0);
                triangleIndices.Add (vertexIndex2);
                triangleIndices.Add (vertexIndex1);
            } else {
                triangleIndices.Add (vertexIndex0);
                triangleIndices.Add (vertexIndex1);
                triangleIndices.Add (vertexIndex2);
            }
        }
    }

    private float GetTransCornerDensity (IVoxelData volume, Vector3 origin, Vector3Int cellPos, byte corner, int side, int step) {
        if (corner < 9) {
            return GetTransCellDensity (volume, origin, cellPos, side, Tables.transFullCorners[corner].x, Tables.transFullCorners[corner].y, 0, step);
        } else {
            byte cornerIndex = (byte) (corner - 9);
            return GetRegularCellDensity (volume, origin, cellPos, side, Tables.transRegularCorners[cornerIndex].x, Tables.transRegularCorners[cornerIndex].y, step);
        }
    }

    private float GetTransCellDensity (IVoxelData volume, Vector3 origin, Vector3Int cellPos, int side, int u, int v, int w, int step) {
        var cellOriginU = 2 * (Tables.transReverseOrientation[side][0].x * (chunkSize - 1) + cellPos.x * Tables.transReverseOrientation[side][1].x + cellPos.y * Tables.transReverseOrientation[side][2].x + cellPos.z * Tables.transReverseOrientation[side][3].x);
        var cellOriginV = 2 * (Tables.transReverseOrientation[side][0].y * (chunkSize - 1) + cellPos.x * Tables.transReverseOrientation[side][1].y + cellPos.y * Tables.transReverseOrientation[side][2].y + cellPos.z * Tables.transReverseOrientation[side][3].y);
        var transCellPos = new Vector3 (
            (Tables.transFullFaceOrientation[side][0].x * chunkSize + (u + cellOriginU) * 0.5f * Tables.transFullFaceOrientation[side][1].x + (v + cellOriginV) * 0.5f * Tables.transFullFaceOrientation[side][2].x + w * 0.5f * Tables.transFullFaceOrientation[side][3].x) * step,
            (Tables.transFullFaceOrientation[side][0].y * chunkSize + (u + cellOriginU) * 0.5f * Tables.transFullFaceOrientation[side][1].y + (v + cellOriginV) * 0.5f * Tables.transFullFaceOrientation[side][2].y + w * 0.5f * Tables.transFullFaceOrientation[side][3].y) * step,
            (Tables.transFullFaceOrientation[side][0].z * chunkSize + (u + cellOriginU) * 0.5f * Tables.transFullFaceOrientation[side][1].z + (v + cellOriginV) * 0.5f * Tables.transFullFaceOrientation[side][2].z + w * 0.5f * Tables.transFullFaceOrientation[side][3].z) * step
        );
        // if (step > 1) {

        //     Debug.Log (transCellPos);
        // }
        float density = volume[origin + new Vector3Int ((int) transCellPos.x, (int) transCellPos.y, (int) transCellPos.z)].density;
        return density;
    }

    private float GetRegularCellDensity (IVoxelData volume, Vector3 origin, Vector3Int cellPos, int side, int u, int v, int step) {
        var coords = new Vector3Int (
            cellPos.x + Tables.transFullFaceOrientation[side][0].x + u * Tables.transFullFaceOrientation[side][1].x + v * Tables.transFullFaceOrientation[side][2].x,
            cellPos.y + Tables.transFullFaceOrientation[side][0].y + u * Tables.transFullFaceOrientation[side][1].y + v * Tables.transFullFaceOrientation[side][2].y,
            cellPos.z + Tables.transFullFaceOrientation[side][0].z + u * Tables.transFullFaceOrientation[side][1].z + v * Tables.transFullFaceOrientation[side][2].z
        );
        float density = volume[origin + (coords * step)].density;
        return density;
    }

    private Vector3 GetTransCornerPos (Vector3Int cellPos, byte corner, int side, int step) {
        if (corner < 9) {
            return GetTransFullFaceCornerPos (cellPos, side, Tables.transFullCorners[corner].x, Tables.transFullCorners[corner].y, step);
        } else {
            byte cornerIndex = (byte) (corner - 9);
            return GetRegularCornerPos (cellPos, side, Tables.transRegularCorners[cornerIndex].x, Tables.transRegularCorners[cornerIndex].y, step);
        }
    }

    private Vector3 GetTransFullFaceCornerPos (Vector3 cellPos, int side, int u, int v, int step) {
        var cornerPos = new Vector3 (
            cellPos.x + (float) (Tables.transFullFaceOrientation[side][0].x) + (u * 0.5f) * Tables.transFullFaceOrientation[side][1].x + (v * 0.5f) * Tables.transFullFaceOrientation[side][2].x,
            cellPos.y + (float) (Tables.transFullFaceOrientation[side][0].y) + (u * 0.5f) * Tables.transFullFaceOrientation[side][1].y + (v * 0.5f) * Tables.transFullFaceOrientation[side][2].y,
            cellPos.z + (float) (Tables.transFullFaceOrientation[side][0].z) + (u * 0.5f) * Tables.transFullFaceOrientation[side][1].z + (v * 0.5f) * Tables.transFullFaceOrientation[side][2].z
        );
        return cornerPos * step;

    }

    private Vector3 GetRegularCornerPos (Vector3Int cellPos, int side, int u, int v, int step) {
        var cornerPos = new Vector3Int (
            cellPos.x + Tables.transFullFaceOrientation[side][0].x + u * Tables.transFullFaceOrientation[side][1].x + v * Tables.transFullFaceOrientation[side][2].x,
            cellPos.y + Tables.transFullFaceOrientation[side][0].y + u * Tables.transFullFaceOrientation[side][1].y + v * Tables.transFullFaceOrientation[side][2].y,
            cellPos.z + Tables.transFullFaceOrientation[side][0].z + u * Tables.transFullFaceOrientation[side][1].z + v * Tables.transFullFaceOrientation[side][2].z
        );
        Vector3 pos = ShiftRegularCornerPos (cornerPos, new Vector3 (cornerPos.x, cornerPos.y, cornerPos.z) * step, step);
        return pos;
    }

    private Vector3 ShiftRegularCornerPos (Vector3Int cornerOffset, Vector3 cornerPos, int step) {
        if (cornerOffset.x == 0) {
            cornerPos.x += shiftFactor * step;
        } else if (cornerOffset.x == chunkSize) {
            cornerPos.x -= shiftFactor * step;
        }

        if (cornerOffset.y == 0) {
            cornerPos.y += shiftFactor * step;
        } else if (cornerOffset.y == chunkSize) {
            cornerPos.y -= shiftFactor * step;
        }

        if (cornerOffset.z == 0) {
            cornerPos.z += shiftFactor * step;
        } else if (cornerOffset.z == chunkSize) {
            cornerPos.z -= shiftFactor * step;
        }
        return cornerPos;
    }
    private Vector3 GetTransCornerNormal (IVoxelData volume, Vector3 origin, Vector3Int cellPos, int side, byte corner, int step) {
        if (corner < 9) {
            return new Vector3 (
                GetTransCellDensity (volume, origin, cellPos, side, Tables.transFullCorners[corner].x + Tables.transReverseOrientation[side][1].x, Tables.transFullCorners[corner].y + Tables.transReverseOrientation[side][1].y, Tables.transReverseOrientation[side][1].z, step) - GetTransCellDensity (volume, origin, cellPos, side, Tables.transFullCorners[corner].x - Tables.transReverseOrientation[side][1].x, Tables.transFullCorners[corner].y - Tables.transReverseOrientation[side][1].y, -Tables.transReverseOrientation[side][1].z, step),
                GetTransCellDensity (volume, origin, cellPos, side, Tables.transFullCorners[corner].x + Tables.transReverseOrientation[side][2].x, Tables.transFullCorners[corner].y + Tables.transReverseOrientation[side][2].y, Tables.transReverseOrientation[side][2].z, step) - GetTransCellDensity (volume, origin, cellPos, side, Tables.transFullCorners[corner].x - Tables.transReverseOrientation[side][2].x, Tables.transFullCorners[corner].y - Tables.transReverseOrientation[side][2].y, -Tables.transReverseOrientation[side][2].z, step),
                GetTransCellDensity (volume, origin, cellPos, side, Tables.transFullCorners[corner].x + Tables.transReverseOrientation[side][3].x, Tables.transFullCorners[corner].y + Tables.transReverseOrientation[side][3].y, Tables.transReverseOrientation[side][3].z, step) - GetTransCellDensity (volume, origin, cellPos, side, Tables.transFullCorners[corner].x - Tables.transReverseOrientation[side][3].x, Tables.transFullCorners[corner].y - Tables.transReverseOrientation[side][3].y, -Tables.transReverseOrientation[side][3].z, step)
            );
        } else {
            byte cornerIndex = (byte) (corner - 9);
            return GetRegularCornerNormal (volume, origin, new Vector3Int (
                cellPos.x + (Tables.transFullFaceOrientation[side][0].x + Tables.transRegularCorners[cornerIndex].x * Tables.transFullFaceOrientation[side][1].x + Tables.transRegularCorners[cornerIndex].y * Tables.transFullFaceOrientation[side][2].x),
                cellPos.y + (Tables.transFullFaceOrientation[side][0].y + Tables.transRegularCorners[cornerIndex].x * Tables.transFullFaceOrientation[side][1].y + Tables.transRegularCorners[cornerIndex].y * Tables.transFullFaceOrientation[side][2].y),
                cellPos.z + (Tables.transFullFaceOrientation[side][0].z + Tables.transRegularCorners[cornerIndex].x * Tables.transFullFaceOrientation[side][1].z + Tables.transRegularCorners[cornerIndex].y * Tables.transFullFaceOrientation[side][2].z)
            ), step);
        }
    }

    private Vector3 GetRegularCornerNormal (IVoxelData volume, Vector3 origin, Vector3Int cornerPos, int step) {
        float dx = volume[origin + ((new Vector3Int (cornerPos.x + 1, cornerPos.y, cornerPos.z)) * step)].density - volume[origin + ((new Vector3Int (cornerPos.x - 1, cornerPos.y, cornerPos.z)) * step)].density;
        float dy = volume[origin + ((new Vector3Int (cornerPos.x, cornerPos.y + 1, cornerPos.z)) * step)].density - volume[origin + ((new Vector3Int (cornerPos.x, cornerPos.y - 1, cornerPos.z)) * step)].density;
        float dz = volume[origin + ((new Vector3Int (cornerPos.x, cornerPos.y, cornerPos.z + 1)) * step)].density - volume[origin + ((new Vector3Int (cornerPos.x, cornerPos.y, cornerPos.z - 1)) * step)].density;
        return new Vector3 (dx, dy, dz);

    }

    private Vector3 GetVertexNormal (Vector3 ANormal, Vector3 BNormal, float lerpFactor) {
        var dx = ANormal.x + lerpFactor * (BNormal.x - ANormal.x);
        var dy = ANormal.y + lerpFactor * (BNormal.y - ANormal.y);
        var dz = ANormal.z + lerpFactor * (BNormal.z - ANormal.z);
        return new Vector3 (-dx, -dy, -dz).normalized;
    }
}