using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced : VoxelMeshGenerator {
    private float isoLevel;
    private int chunkSize;
    private static float shiftFactor = 0.30f;

    public override void Init (MeshGeneratorSettings settings) {
        this.isoLevel = settings.isoLevel;
        this.chunkSize = settings.chunkSize;

    }

    public override MeshData GenerateMesh (Voxel[] voxelData, int size, int step, float scale) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();
        List<Vector3> normals = new List<Vector3> ();

        for (int z = 0; z < chunkSize; z++)
            for (int y = 0; y < chunkSize; y++)
                for (int x = 0; x < chunkSize; x++) {
                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (voxelData, cellPos, size, step, scale, ref vertices, ref triangleIndices, ref normals);
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
                        PolygonizeTransitionCell (voxelData, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals, u, v, side, step);
                    }
        }

        return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals.ToArray ());
    }

    internal void PolygonizeCell (Voxel[] voxels, Vector3Int cellPos, int size, int step, float scale, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals) {
        float[] cubeDensities = new float[8];
        byte caseCode = 0;
        byte addToCaseCode = 1;
        for (int i = 0; i < cubeDensities.Length; i++) {
            cubeDensities[i] = voxels[Util.Map3DTo1D ((cellPos + (Tables.CornerIndex[i]) + Vector3Int.one) * step, size)].density;
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

            Vector3 cornerANormal = GetRegularCornerNormal (voxels, p0Int, size, step);
            Vector3 cornerBNormal = GetRegularCornerNormal (voxels, p1Int, size, step);

            if (step > 1) {
                p0 = ShiftRegularCornerPos (p0Int, p0, size, step);
                p1 = ShiftRegularCornerPos (p1Int, p1, size, step);
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

    internal void PolygonizeTransitionCell (Voxel[] voxels, Vector3Int cellPos, int size, float scale, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int u, int v, int side, int step) {
        float[] transFullFaceDensities = new float[9];

        int[] caseCodeCoeffs = new int[9] { 0x01, 0x02, 0x04, 0x80, 0x100, 0x08, 0x40, 0x20, 0x10 };
        int transCaseCode = 0;
        for (byte i = 0; i < 9; i++) {
            transFullFaceDensities[i] = GetTransCornerDensity (voxels, cellPos, size, i, side, step);
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

            Vector3 p0 = GetTransCornerPos (cellPos, cornerA, side, size, step);
            Vector3 p1 = GetTransCornerPos (cellPos, cornerB, side, size, step);

            Vector3 cornerANormal = GetTransCornerNormal (voxels, cellPos, side, cornerA, size, step);
            Vector3 cornerBNormal = GetTransCornerNormal (voxels, cellPos, side, cornerB, size, step);

            float densityA = cornerA < 9 ? transFullFaceDensities[cornerA] : GetTransCornerDensity (voxels, cellPos, size, cornerA, side, step);
            float densityB = cornerB < 9 ? transFullFaceDensities[cornerB] : GetTransCornerDensity (voxels, cellPos, size, cornerB, side, step);

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

    private float GetTransCornerDensity (Voxel[] voxels, Vector3Int cellPos, int size, byte corner, int side, int step) {
        if (corner < 9) {
            return GetTransCellDensity (voxels, cellPos, size, side, Tables.transFullCorners[corner].x, Tables.transFullCorners[corner].y, 0, step);
        } else {
            byte cornerIndex = (byte) (corner - 9);
            return GetRegularCellDensity (voxels, cellPos, size, side, Tables.transRegularCorners[cornerIndex].x, Tables.transRegularCorners[cornerIndex].y, step);
        }
    }

    private float GetTransCellDensity (Voxel[] voxels, Vector3Int cellPos, int size, int side, int u, int v, int w, int step) {
        var cellOriginU = 2 * (Tables.transReverseOrientation[side][0].x * (chunkSize - 1) + cellPos.x * Tables.transReverseOrientation[side][1].x + cellPos.y * Tables.transReverseOrientation[side][2].x + cellPos.z * Tables.transReverseOrientation[side][3].x);
        var cellOriginV = 2 * (Tables.transReverseOrientation[side][0].y * (chunkSize - 1) + cellPos.x * Tables.transReverseOrientation[side][1].y + cellPos.y * Tables.transReverseOrientation[side][2].y + cellPos.z * Tables.transReverseOrientation[side][3].y);
        var transCellPos = new Vector3 (
            ((Tables.transFullFaceOrientation[side][0].x * chunkSize + (u + cellOriginU) * 0.5f * Tables.transFullFaceOrientation[side][1].x + (v + cellOriginV) * 0.5f * Tables.transFullFaceOrientation[side][2].x + w * 0.5f * Tables.transFullFaceOrientation[side][3].x) + 1) * step,
            ((Tables.transFullFaceOrientation[side][0].y * chunkSize + (u + cellOriginU) * 0.5f * Tables.transFullFaceOrientation[side][1].y + (v + cellOriginV) * 0.5f * Tables.transFullFaceOrientation[side][2].y + w * 0.5f * Tables.transFullFaceOrientation[side][3].y) + 1) * step,
            ((Tables.transFullFaceOrientation[side][0].z * chunkSize + (u + cellOriginU) * 0.5f * Tables.transFullFaceOrientation[side][1].z + (v + cellOriginV) * 0.5f * Tables.transFullFaceOrientation[side][2].z + w * 0.5f * Tables.transFullFaceOrientation[side][3].z) + 1) * step
        );
        float density = voxels[Util.Map3DTo1D (new Vector3Int ((int) transCellPos.x, (int) transCellPos.y, (int) transCellPos.z), size)].density;
        return density;
    }

    private float GetRegularCellDensity (Voxel[] voxels, Vector3Int cellPos, int size, int side, int u, int v, int step) {
        var coords = new Vector3Int (
            cellPos.x + Tables.transFullFaceOrientation[side][0].x + u * Tables.transFullFaceOrientation[side][1].x + v * Tables.transFullFaceOrientation[side][2].x,
            cellPos.y + Tables.transFullFaceOrientation[side][0].y + u * Tables.transFullFaceOrientation[side][1].y + v * Tables.transFullFaceOrientation[side][2].y,
            cellPos.z + Tables.transFullFaceOrientation[side][0].z + u * Tables.transFullFaceOrientation[side][1].z + v * Tables.transFullFaceOrientation[side][2].z
        );
        var density = voxels[Util.Map3DTo1D ((coords + Vector3Int.one) * step, size)].density;
        return density;
    }

    private Vector3 GetTransCornerPos (Vector3Int cellPos, byte corner, int side, int size, int step) {
        if (corner < 9) {
            return GetTransFullFaceCornerPos (cellPos, side, Tables.transFullCorners[corner].x, Tables.transFullCorners[corner].y, step);
        } else {
            byte cornerIndex = (byte) (corner - 9);
            return GetRegularCornerPos (cellPos, side, Tables.transRegularCorners[cornerIndex].x, Tables.transRegularCorners[cornerIndex].y, size, step);
        }
    }

    private Vector3 GetTransFullFaceCornerPos (Vector3 cellPos, int side, int u, int v, int step) {
        var cornerPos = new Vector3 (
            cellPos.x + (float) (Tables.transFullFaceOrientation[side][0].x) + (u * 0.5f) * Tables.transFullFaceOrientation[side][1].x + (v * 0.5f) * Tables.transFullFaceOrientation[side][2].x,
            cellPos.y + (float) (Tables.transFullFaceOrientation[side][0].y) + (u * 0.5f) * Tables.transFullFaceOrientation[side][1].y + (v * 0.5f) * Tables.transFullFaceOrientation[side][2].y,
            cellPos.z + (float) (Tables.transFullFaceOrientation[side][0].z) + (u * 0.5f) * Tables.transFullFaceOrientation[side][1].z + (v * 0.5f) * Tables.transFullFaceOrientation[side][2].z
        ) * step;
        return cornerPos;

    }

    private Vector3 GetRegularCornerPos (Vector3Int cellPos, int side, int u, int v, int size, int step) {
        var cornerPos = new Vector3Int (
            cellPos.x + Tables.transFullFaceOrientation[side][0].x + u * Tables.transFullFaceOrientation[side][1].x + v * Tables.transFullFaceOrientation[side][2].x,
            cellPos.y + Tables.transFullFaceOrientation[side][0].y + u * Tables.transFullFaceOrientation[side][1].y + v * Tables.transFullFaceOrientation[side][2].y,
            cellPos.z + Tables.transFullFaceOrientation[side][0].z + u * Tables.transFullFaceOrientation[side][1].z + v * Tables.transFullFaceOrientation[side][2].z
        );
        Vector3 pos = ShiftRegularCornerPos (cornerPos, new Vector3 (cornerPos.x, cornerPos.y, cornerPos.z) * step, size, step);
        return pos;
    }

    private Vector3 ShiftRegularCornerPos (Vector3Int cornerOffset, Vector3 cornerPos, int size, int step) {
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

    private Vector3 GetTransCornerNormal (Voxel[] voxels, Vector3Int cellPos, int side, byte corner, int size, int step) {
        if (corner < 9) {
            return new Vector3 (
                GetTransCellDensity (voxels, cellPos, size, side, Tables.transFullCorners[corner].x + Tables.transReverseOrientation[side][1].x, Tables.transFullCorners[corner].y + Tables.transReverseOrientation[side][1].y, Tables.transReverseOrientation[side][1].z, step) - GetTransCellDensity (voxels, cellPos, size, side, Tables.transFullCorners[corner].x - Tables.transReverseOrientation[side][1].x, Tables.transFullCorners[corner].y - Tables.transReverseOrientation[side][1].y, -Tables.transReverseOrientation[side][1].z, step),
                GetTransCellDensity (voxels, cellPos, size, side, Tables.transFullCorners[corner].x + Tables.transReverseOrientation[side][2].x, Tables.transFullCorners[corner].y + Tables.transReverseOrientation[side][2].y, Tables.transReverseOrientation[side][2].z, step) - GetTransCellDensity (voxels, cellPos, size, side, Tables.transFullCorners[corner].x - Tables.transReverseOrientation[side][2].x, Tables.transFullCorners[corner].y - Tables.transReverseOrientation[side][2].y, -Tables.transReverseOrientation[side][2].z, step),
                GetTransCellDensity (voxels, cellPos, size, side, Tables.transFullCorners[corner].x + Tables.transReverseOrientation[side][3].x, Tables.transFullCorners[corner].y + Tables.transReverseOrientation[side][3].y, Tables.transReverseOrientation[side][3].z, step) - GetTransCellDensity (voxels, cellPos, size, side, Tables.transFullCorners[corner].x - Tables.transReverseOrientation[side][3].x, Tables.transFullCorners[corner].y - Tables.transReverseOrientation[side][3].y, -Tables.transReverseOrientation[side][3].z, step)
            );
        } else {
            byte cornerIndex = (byte) (corner - 9);
            return GetRegularCornerNormal (voxels, new Vector3Int (
                cellPos.x + (Tables.transFullFaceOrientation[side][0].x + Tables.transRegularCorners[cornerIndex].x * Tables.transFullFaceOrientation[side][1].x + Tables.transRegularCorners[cornerIndex].y * Tables.transFullFaceOrientation[side][2].x),
                cellPos.y + (Tables.transFullFaceOrientation[side][0].y + Tables.transRegularCorners[cornerIndex].x * Tables.transFullFaceOrientation[side][1].y + Tables.transRegularCorners[cornerIndex].y * Tables.transFullFaceOrientation[side][2].y),
                cellPos.z + (Tables.transFullFaceOrientation[side][0].z + Tables.transRegularCorners[cornerIndex].x * Tables.transFullFaceOrientation[side][1].z + Tables.transRegularCorners[cornerIndex].y * Tables.transFullFaceOrientation[side][2].z)
            ), size, step);
        }
    }

    private Vector3 GetRegularCornerNormal (Voxel[] voxels, Vector3Int cornerPos, int size, int step) {
        float dx = voxels[Util.Map3DTo1D ((new Vector3Int (cornerPos.x + 1, cornerPos.y, cornerPos.z) + Vector3Int.one) * step, size)].density - voxels[Util.Map3DTo1D ((new Vector3Int (cornerPos.x - 1, cornerPos.y, cornerPos.z) + Vector3Int.one) * step, size)].density;
        float dy = voxels[Util.Map3DTo1D ((new Vector3Int (cornerPos.x, cornerPos.y + 1, cornerPos.z) + Vector3Int.one) * step, size)].density - voxels[Util.Map3DTo1D ((new Vector3Int (cornerPos.x, cornerPos.y - 1, cornerPos.z) + Vector3Int.one) * step, size)].density;
        float dz = voxels[Util.Map3DTo1D ((new Vector3Int (cornerPos.x, cornerPos.y, cornerPos.z + 1) + Vector3Int.one) * step, size)].density - voxels[Util.Map3DTo1D ((new Vector3Int (cornerPos.x, cornerPos.y, cornerPos.z - 1) + Vector3Int.one) * step, size)].density;
        return new Vector3 (dx, dy, dz);

    }

    private Vector3 GetVertexNormal (Vector3 ANormal, Vector3 BNormal, float lerpFactor) {
        var dx = ANormal.x + lerpFactor * (BNormal.x - ANormal.x);
        var dy = ANormal.y + lerpFactor * (BNormal.y - ANormal.y);
        var dz = ANormal.z + lerpFactor * (BNormal.z - ANormal.z);
        return new Vector3 (-dx, -dy, -dz).normalized;
    }
}