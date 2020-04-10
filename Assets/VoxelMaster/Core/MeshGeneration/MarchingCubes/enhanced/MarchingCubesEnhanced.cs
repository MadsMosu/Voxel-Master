using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced : VoxelMeshGenerator {
    private float isoLevel;
    private int chunkSize;
    // private int chunkSizeWithLod, transvoxelCount, startTransCells, endTransCells, chunkSize;
    private static Vector3Int Vector3IntForward = new Vector3Int (0, 0, 1);

    // private static Vector3Int[][] transCellFaces = new Vector3Int[3][] {
    //     new Vector3Int[9] { new Vector3Int (0, 0, 0), new Vector3Int (0, 1, 0), new Vector3Int (0, 2, 0), new Vector3Int (0, 2, 1), new Vector3Int (0, 2, 2), new Vector3Int (0, 1, 2), new Vector3Int (0, 0, 2), new Vector3Int (0, 0, 1), new Vector3Int (0, 1, 1) }, //left and right face
    //     new Vector3Int[9] { new Vector3Int (0, 0, 0), new Vector3Int (1, 0, 0), new Vector3Int (2, 0, 0), new Vector3Int (2, 0, 1), new Vector3Int (2, 0, 2), new Vector3Int (1, 0, 2), new Vector3Int (0, 0, 2), new Vector3Int (0, 0, 1), new Vector3Int (1, 0, 1) }, //bottom and top face
    //     new Vector3Int[9] { new Vector3Int (0, 0, 0), new Vector3Int (1, 0, 0), new Vector3Int (2, 0, 0), new Vector3Int (2, 1, 0), new Vector3Int (2, 2, 0), new Vector3Int (1, 2, 0), new Vector3Int (0, 2, 0), new Vector3Int (0, 1, 0), new Vector3Int (1, 1, 0) }, //backward and forward face
    // };

    // private static bool[] invertTriangles = new bool[] {
    //     false,
    //     true,
    //     true,
    //     false,
    //     false,
    //     true
    // };

    // private Vector3Int[][] chunkTransFaceIterations;

    public override void Init (MeshGeneratorSettings settings) {
        this.isoLevel = settings.isoLevel;
        this.chunkSize = settings.chunkSize;
        // this.chunkSizeWithLod = (settings.chunkSize + 1) * 2;
        // this.transvoxelCount = chunkSizeWithLod * chunkSizeWithLod;

        // this.startTransCells = chunkSizeWithLod - 2;
        // this.endTransCells = chunkSizeWithLod - 1;

        // this.chunkTransFaceIterations =
    }

    public override MeshData GenerateMesh (Voxel[] voxelData, int size, int step, float scale) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();
        List<Vector3> normals = new List<Vector3> ();

        for (int z = 1; z < size - 2; z += step)
            for (int y = 1; y < size - 2; y += step)
                for (int x = 1; x < size - 2; x += step) {
                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (voxelData, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals);
                }

        // if (scale > 1) {

        //     var chunksizeWithEdge = size - 1;
        //     var chunkSizeWithoutEdge = size - 2;

        //     var faceIterations = new Vector3Int[][] {
        //         new Vector3Int[2] { new Vector3Int (1, 1, 1), new Vector3Int (1, chunkSizeWithoutEdge, chunkSizeWithoutEdge) },
        //         new Vector3Int[2] { new Vector3Int (chunkSizeWithoutEdge, 1, 1), new Vector3Int (chunksizeWithEdge, chunkSizeWithoutEdge, chunkSizeWithoutEdge) },
        //         new Vector3Int[2] { new Vector3Int (1, 1, 1), new Vector3Int (chunkSizeWithoutEdge, 2, chunkSizeWithoutEdge) },
        //         new Vector3Int[2] { new Vector3Int (1, chunkSizeWithoutEdge, 1), new Vector3Int (chunkSizeWithoutEdge, chunksizeWithEdge, chunkSizeWithoutEdge) },
        //         new Vector3Int[2] { new Vector3Int (1, 1, 1), new Vector3Int (chunkSizeWithoutEdge, chunkSizeWithoutEdge, 2) },
        //         new Vector3Int[2] { new Vector3Int (1, 1, chunkSizeWithoutEdge), new Vector3Int (chunkSizeWithoutEdge, chunkSizeWithoutEdge, chunksizeWithEdge) }
        //     };

        //     for (int side = 0; side < 6; side++) {

        //         Vector3Int transStart = faceIterations[side][0];
        //         Vector3Int transEnd = faceIterations[side][1];
        //         Vector3Int[] transOffsets = transCellFaces[side / 2];
        //         bool flipWinding = invertTriangles[side];

        //         for (int x = transStart.x; x < transEnd.x; x += 2)
        //             for (int y = transStart.y; y < transEnd.y; y += 2)
        //                 for (int z = transStart.z; z < transEnd.z; z += 2) {
        //                     Vector3Int cellPos = new Vector3Int (x, y, z);
        //                     PolygonizeTransitionCell (voxelData, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals, side, transOffsets, flipWinding);
        //                 }
        //     }
        // }
        return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals.ToArray ());
    }

    internal void PolygonizeCell (Voxel[] volume, Vector3Int cellPos, int size, float scale, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals) {

        float[] cubeDensities = new float[8];
        byte caseCode = 0;
        byte addToCaseCode = 1;
        for (int i = 0; i < cubeDensities.Length; i++) {
            cubeDensities[i] = volume[Util.Map3DTo1D (cellPos + Tables.CornerIndex[i], size)].density;
            if (cubeDensities[i] < isoLevel) {
                caseCode |= addToCaseCode;
            }
            addToCaseCode *= 2;
        }
        cellPos -= Vector3Int.one;

        if (caseCode == 0 || caseCode == 255) return;

        byte regularCellClass = Tables.RegularCellClass[caseCode];
        Tables.RegularCell regularCell = Tables.RegularCellData[regularCellClass];
        ushort[] vertexData = Tables.RegularVertexData[caseCode];

        byte[] cellIndices = regularCell.GetIndices ();
        if (cellIndices == null) return;
        int[] indicesMapping = new int[cellIndices.Length]; //maps the added indices to the vertexData indices

        long vertexCount = regularCell.GetVertexCount ();
        long triangleCount = regularCell.GetTriangleCount ();
        for (int i = 0; i < vertexCount; i++) {
            byte edgeCode = (byte) (vertexData[i] & 0xFF);

            byte cornerA = (byte) ((edgeCode >> 4) & 0x0F);
            byte cornerB = (byte) (edgeCode & 0x0F);

            float densityA = cubeDensities[cornerA];
            float densityB = cubeDensities[cornerB];

            var p0Int = cellPos + Tables.CornerIndex[cornerA];
            var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
            var p1Int = cellPos + Tables.CornerIndex[cornerB];
            var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

            float lerpFactor = (isoLevel - densityA) / (densityB - densityA);
            var Q = p0 + lerpFactor * (p1 - p0);

            // normals.Add (GetNormal (cellPos + Tables.CornerIndex[cornerA], cellPos + Tables.CornerIndex[cornerB], volume, lerpFactor));
            vertices.Add (Q * scale);
            indicesMapping[i] = vertices.Count - 1;
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
            triangleIndices.Add (vertexIndex1);
            triangleIndices.Add (vertexIndex2);
        }

        if (scale > 1) {
            Debug.Log ($"before: ${vertices.Count}");
            if (cellPos.z == 0) { //Z+
                // cellPos += new Vector3Int (0, 0, -1);
                PolygonizeTransitionCell (volume, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals, new int[] { 1, 5, 0, 4 }, cubeDensities);
            }
            if (cellPos.z == size - 3) { //Z-
                // cellPos += new Vector3Int (0, 0, 1);
                PolygonizeTransitionCell (volume, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals, new int[] { 7, 3, 6, 2 }, cubeDensities);
            }
            if (cellPos.y == 0) { //Y+
                // cellPos += new Vector3Int (0, -1, 0);
                PolygonizeTransitionCell (volume, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals, new int[] { 1, 0, 3, 2 }, cubeDensities);
            }
            if (cellPos.y == size - 3) { //Y-
                // cellPos += new Vector3Int (0, 1, 0);
                PolygonizeTransitionCell (volume, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals, new int[] { 4, 5, 6, 7 }, cubeDensities);
            }
            if (cellPos.x == 0) { //X+
                // cellPos += new Vector3Int (-1, 0, 0);
                PolygonizeTransitionCell (volume, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals, new int[] { 0, 4, 2, 6 }, cubeDensities);
            }
            if (cellPos.x == size - 3) { //X-
                // cellPos += new Vector3Int (1, 0, 0);
                PolygonizeTransitionCell (volume, cellPos, size, scale, ref vertices, ref triangleIndices, ref normals, new int[] { 5, 1, 7, 3 }, cubeDensities);
            }
            Debug.Log ($"after: ${vertices.Count}");
        }

    }

    internal void PolygonizeTransitionCell (Voxel[] volume, Vector3Int cellPos, int size, float scale, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int[] halfFaceCorners, float[] cubeDensities) {
        cellPos += Vector3Int.one;
        var d1 = Mathf.Lerp (cubeDensities[halfFaceCorners[1]], cubeDensities[halfFaceCorners[0]], 0.5f);
        var d3 = Mathf.Lerp (cubeDensities[halfFaceCorners[2]], cubeDensities[halfFaceCorners[0]], 0.5f);
        var d5 = Mathf.Lerp (cubeDensities[halfFaceCorners[3]], cubeDensities[halfFaceCorners[1]], 0.5f);
        var d4 = Mathf.Lerp (d5, d3, 0.5f);
        var d7 = Mathf.Lerp (cubeDensities[halfFaceCorners[3]], cubeDensities[halfFaceCorners[2]], 0.5f);

        var transCellDensities = new float[] {
            cubeDensities[halfFaceCorners[0]],
            d1,
            cubeDensities[halfFaceCorners[1]],
            d3,
            d4,
            d5,
            cubeDensities[halfFaceCorners[2]],
            d7,
            cubeDensities[halfFaceCorners[3]],

            cubeDensities[halfFaceCorners[0]],
            cubeDensities[halfFaceCorners[1]],
            cubeDensities[halfFaceCorners[2]],
            cubeDensities[halfFaceCorners[3]]
        };

        int[] caseCodeCoeffs = new int[9] { 0x01, 0x02, 0x04, 0x80, 0x100, 0x08, 0x40, 0x20, 0x10 };
        int transCaseCode = 0;
        for (int i = 0; i < 9; i++) {
            if (transCellDensities[i] < isoLevel) {
                transCaseCode |= caseCodeCoeffs[i];
            }
        }

        if (transCaseCode == 0 || transCaseCode == 511) return;

        var pos1 = GetMiddlePos (Tables.CornerIndex[halfFaceCorners[1]], Tables.CornerIndex[halfFaceCorners[0]]);
        var pos3 = GetMiddlePos (Tables.CornerIndex[halfFaceCorners[2]], Tables.CornerIndex[halfFaceCorners[0]]);
        var pos5 = GetMiddlePos (Tables.CornerIndex[halfFaceCorners[3]], Tables.CornerIndex[halfFaceCorners[1]]);
        var pos4 = GetMiddlePos (pos5, pos3);
        var pos7 = GetMiddlePos (Tables.CornerIndex[halfFaceCorners[3]], Tables.CornerIndex[halfFaceCorners[2]]);

        var transCornerOffset = new Vector3[] {
            Tables.CornerIndex[halfFaceCorners[0]],
            pos1,
            Tables.CornerIndex[halfFaceCorners[1]],
            pos3,
            pos4,
            pos5,
            Tables.CornerIndex[halfFaceCorners[2]],
            pos7,
            Tables.CornerIndex[halfFaceCorners[3]],

            Tables.CornerIndex[halfFaceCorners[0]],
            Tables.CornerIndex[halfFaceCorners[1]],
            Tables.CornerIndex[halfFaceCorners[2]],
            Tables.CornerIndex[halfFaceCorners[3]],
        };

        byte transCellClass = Tables.TransitionCellClass[transCaseCode];
        Debug.Log (transCaseCode);
        Tables.TransitionCell transCell = Tables.TransitionCellData[transCellClass & 0x7F];

        byte[] cellIndices = transCell.GetIndices ();
        // if (cellIndices == null) return;

        int[] indicesMapping = new int[cellIndices.Length];
        bool flipWinding = (transCellClass & 128) != 0;
        // bool flipWinding = (transCellClass >> 7) != 0;

        long vertexCount = transCell.GetVertexCount ();
        long triangleCount = transCell.GetTriangleCount ();

        for (int i = 0; i < vertexCount; i++) {
            byte edgeCode = (byte) (Tables.TransitionVertexData[transCaseCode][i]);

            byte cornerA = (byte) ((edgeCode >> 4) & 0x0F);
            byte cornerB = (byte) (edgeCode & 0x0F);
            Debug.Assert (cornerA != cornerB);
            float densityA = transCellDensities[cornerA];
            float densityB = transCellDensities[cornerB];

            Vector3 p0 = cellPos + transCornerOffset[cornerA];
            Vector3 p1 = cellPos + transCornerOffset[cornerB];

            float lerpFactor = (isoLevel - densityA) / (densityB - densityA);
            var Q = p0 + lerpFactor * (p1 - p0);

            vertices.Add (Q * scale);
            indicesMapping[i] = vertices.Count - 1;
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

    private Vector3 GetMiddlePos (Vector3 p0, Vector3 p1) {
        return (p1 - p0) / 2 + p0;
    }

    // internal void PolygonizeTransitionCell (Voxel[] volume, Vector3Int cellPos, int size, float scale, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int side, Vector3Int[] transOffsets, bool flipWinding) {

    //     float[] halfFaceDensities = new float[9];
    //     int transCaseCode = 0;
    //     byte addToCaseCode = 1;
    //     for (int i = 0; i < 9; i++) {
    //         Vector3Int pos = cellPos + transOffsets[i];
    //         halfFaceDensities[i] = volume[Util.Map3DTo1D (GetTransPosition (pos, side), size)].density;
    //         if (halfFaceDensities[i] < isoLevel) {
    //             transCaseCode |= addToCaseCode;
    //         }
    //         addToCaseCode *= 2;
    //     }

    //     if (transCaseCode == 0 || transCaseCode == 511) return;

    //     byte transCellClass = Tables.TransitionCellClass[transCaseCode];
    //     Tables.TransitionCell transCell = Tables.TransitionCellData[transCellClass & 0x7F];
    //     ushort[] vertexData = Tables.TransitionVertexData[transCaseCode];

    //     byte[] cellIndices = transCell.GetIndices ();
    //     if (cellIndices == null) return;
    //     int[] indicesMapping = new int[cellIndices.Length];

    //     long vertexCount = transCell.GetVertexCount ();
    //     long triangleCount = transCell.GetTriangleCount ();

    //     for (int i = 0; i < vertexCount; i++) {
    //         byte edgeCode = (byte) (vertexData[i]);

    //         byte cornerA = (byte) (edgeCode & 0x0F);
    //         byte cornerB = (byte) ((edgeCode & 0xF0) >> 4);

    //         byte edgeIndex = (byte) ((edgeCode >> 8) & 0x0F);

    //         Vector3Int cornerAHalfFacePos = GetTransCorner (cornerA, transOffsets);
    //         Vector3Int cornerBHalfFacePos = GetTransCorner (cornerB, transOffsets);

    //         float densityA = volume[Util.Map3DTo1D ((cellPos) + cornerAHalfFacePos, size)].density;
    //         float densityB = volume[Util.Map3DTo1D ((cellPos) + cornerBHalfFacePos, size)].density;

    //         Vector3Int p0Int = cellPos + cornerAHalfFacePos;
    //         Vector3 p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
    //         Vector3Int p1Int = cellPos + cornerBHalfFacePos;
    //         Vector3 p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

    //         float lerpFactor = (isoLevel - densityA) / (densityB - densityA);
    //         var Q = p0 + lerpFactor * (p1 - p0);

    //         vertices.Add (Q * scale);
    //         indicesMapping[i] = vertices.Count - 1;
    //     }

    //     for (int t = 0; t < triangleCount; t++) {
    //         int vertexIndex0 = indicesMapping[cellIndices[t * 3]];
    //         int vertexIndex1 = indicesMapping[cellIndices[t * 3 + 1]];
    //         int vertexIndex2 = indicesMapping[cellIndices[t * 3 + 2]];

    //         Vector3 vertex0 = vertices[vertexIndex0];
    //         Vector3 vertex1 = vertices[vertexIndex1];
    //         Vector3 vertex2 = vertices[vertexIndex2];
    //         if (vertex0 == vertex1 || vertex0 == vertex2 || vertex1 == vertex2) continue; //triangle with zero space

    //         byte invert = 1;
    //         if (flipWinding) {
    //             invert = 0;
    //         }

    //         if ((transCellClass >> 7) % 2 == invert) {
    //             triangleIndices.Add (vertexIndex0);
    //             triangleIndices.Add (vertexIndex1);
    //             triangleIndices.Add (vertexIndex2);
    //         } else {
    //             triangleIndices.Add (vertexIndex0);
    //             triangleIndices.Add (vertexIndex2);
    //             triangleIndices.Add (vertexIndex1);
    //         }
    //     }
    // }

    // private static Vector3Int GetTransCorner (byte corner, Vector3Int[] transFaceCorners) {
    //     switch (corner) {
    //         case 0:
    //             return transFaceCorners[0];
    //         case 1:
    //             return transFaceCorners[1];
    //         case 2:
    //             return transFaceCorners[2];
    //         case 3:
    //             return transFaceCorners[7];
    //         case 4:
    //             return transFaceCorners[8];
    //         case 5:
    //             return transFaceCorners[3];
    //         case 6:
    //             return transFaceCorners[6];
    //         case 7:
    //             return transFaceCorners[5];
    //         case 8:
    //             return transFaceCorners[4];
    //         case 9:
    //         case 10:
    //         case 11:
    //         case 12:
    //         default:
    //             return Vector3Int.zero;
    //     }
    // }

    // private static Vector3Int GetTransPosition (Vector3Int pos, int side) {
    //     switch (side) {
    //         case 0: // x-axis
    //         case 1:
    //             return new Vector3Int (0, pos.y, pos.z);
    //         case 2: // y-axis
    //         case 3:
    //             return new Vector3Int (pos.x, 0, pos.z);
    //         case 4: // z-axis
    //         case 5:
    //             return new Vector3Int (pos.x, pos.y, 0);
    //         default:
    //             return Vector3Int.zero;
    //     }
    // }

    private Vector3 GetNormal (Vector3Int a, Vector3Int b, Voxel[] voxels, int size) {

        // float cornerAnx = (volume[cornerAPos + Vector3Int.right].density - volume[cornerAPos - Vector3Int.right].density);
        // float cornerAny = (volume[cornerAPos + Vector3Int.up].density - volume[cornerAPos - Vector3Int.up].density);
        // float cornerAnz = (volume[cornerAPos + Vector3IntForward].density - volume[cornerAPos - Vector3IntForward].density);

        // float cornerBnx = (volume[cornerBPos + Vector3Int.right].density - volume[cornerBPos - Vector3Int.right].density);
        // float cornerBny = (volume[cornerBPos + Vector3Int.up].density - volume[cornerBPos - Vector3Int.up].density);
        // float cornerBnz = (volume[cornerBPos + Vector3IntForward].density - volume[cornerBPos - Vector3IntForward].density);

        // Vector3 normal = new Vector3 (cornerAnx, cornerAny, cornerAnz) * (1f - lerpFactor) + new Vector3 (cornerBnx, cornerBny, cornerBnz) * lerpFactor;
        // return -(normal).normalized;

        float dx = voxels[Util.Map3DTo1D (new Vector3Int (a.x + 1, a.y, a.z), size)].density - voxels[Util.Map3DTo1D (new Vector3Int (b.x - 1, b.y, b.z), size)].density;
        float dy = voxels[Util.Map3DTo1D (new Vector3Int (a.x, a.y + 1, a.z), size)].density - voxels[Util.Map3DTo1D (new Vector3Int (b.x, b.y - 1, b.z), size)].density;
        float dz = voxels[Util.Map3DTo1D (new Vector3Int (a.x, a.y, a.z + 1), size)].density - voxels[Util.Map3DTo1D (new Vector3Int (b.x, b.y, b.z - 1), size)].density;
        return new Vector3 (dx, dy, dz).normalized;
    }
}