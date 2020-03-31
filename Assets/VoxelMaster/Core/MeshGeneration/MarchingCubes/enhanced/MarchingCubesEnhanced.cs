using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced : VoxelMeshGenerator {
    private float isoLevel;
    private int chunkSizeWithLod, transvoxelCount, startTransCells, endTransCells, chunkSize;
    private static Vector3Int Vector3IntForward = new Vector3Int (0, 0, 1);

    //Represents the 9 vertices on the three possible half-resolution faces of a cell (left, forward and upwards face)
    private static Vector3Int[][] transCellFaces = new Vector3Int[3][] {
        new Vector3Int[9] { new Vector3Int (0, 0, 0), new Vector3Int (0, 1, 0), new Vector3Int (0, 2, 0), new Vector3Int (0, 2, 1), new Vector3Int (0, 2, 2), new Vector3Int (0, 1, 2), new Vector3Int (0, 0, 2), new Vector3Int (0, 0, 1), new Vector3Int (0, 1, 1) }, //left and right face
        new Vector3Int[9] { new Vector3Int (0, 0, 0), new Vector3Int (1, 0, 0), new Vector3Int (2, 0, 0), new Vector3Int (2, 0, 1), new Vector3Int (2, 0, 2), new Vector3Int (1, 0, 2), new Vector3Int (0, 0, 2), new Vector3Int (0, 0, 1), new Vector3Int (1, 0, 1) }, //bottom and top face
        new Vector3Int[9] { new Vector3Int (0, 0, 0), new Vector3Int (1, 0, 0), new Vector3Int (2, 0, 0), new Vector3Int (2, 1, 0), new Vector3Int (2, 2, 0), new Vector3Int (1, 2, 0), new Vector3Int (0, 2, 0), new Vector3Int (0, 1, 0), new Vector3Int (1, 1, 0) }, //backward and forward face
    };

    private static bool[] invertTriangles = new bool[] {
        false,
        true,
        true,
        false,
        false,
        true
    };

    private Vector3Int[][] chunkTransFaceIterations;

    public override void Init (MeshGeneratorSettings settings) {
        this.isoLevel = settings.isoLevel;
        this.chunkSize = settings.chunkSize;
        this.chunkSizeWithLod = (settings.chunkSize + 1) * 2;
        this.transvoxelCount = chunkSizeWithLod * chunkSizeWithLod;

        this.startTransCells = chunkSizeWithLod - 2;
        this.endTransCells = chunkSizeWithLod - 1;

        this.chunkTransFaceIterations = new Vector3Int[][] {
            new Vector3Int[2] { new Vector3Int (0, 0, 0), new Vector3Int (1, startTransCells, startTransCells) },
            new Vector3Int[2] { new Vector3Int (startTransCells, 0, 0), new Vector3Int (endTransCells, startTransCells, startTransCells) },
            new Vector3Int[2] { new Vector3Int (0, 0, 0), new Vector3Int (startTransCells, 1, startTransCells) },
            new Vector3Int[2] { new Vector3Int (0, startTransCells, 0), new Vector3Int (startTransCells, endTransCells, startTransCells) },
            new Vector3Int[2] { new Vector3Int (0, 0, 0), new Vector3Int (startTransCells, startTransCells, 1) },
            new Vector3Int[2] { new Vector3Int (0, 0, startTransCells), new Vector3Int (startTransCells, startTransCells, endTransCells) }
        };
    }

    public override MeshData GenerateMesh (IVoxelData voxelData, Vector3Int origin, int size, int lod) {
        int numCells = size * size * size;
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();
        List<Vector3> normals = new List<Vector3> ();

        int lodIncrementer = 1 << lod;

        for (int z = 0; z < size; z += lodIncrementer)
            for (int y = 0; y < size; y += lodIncrementer)
                for (int x = 0; x < size; x += lodIncrementer) {
                    // if (x % lodIncrementer == 0 && y % lodIncrementer == 0 && z % lodIncrementer == 0) {
                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (voxelData, origin, cellPos, ref vertices, ref triangleIndices, ref normals, lodIncrementer, lod);
                    // }
                }

        // if (lod > 0) {
        //     Vector3Int transStart = chunkTransFaceIterations[lod][0];
        //     Vector3Int transEnd = chunkTransFaceIterations[lod][1];
        //     for (int x = transStart.x; x < transEnd.x; x += 2)
        //         for (int y = transStart.y; y < transEnd.y; y += 2)
        //             for (int z = transStart.z; z < transEnd.z; z += 2) {
        //                 Vector3Int cellPos = new Vector3Int (x, y, z);
        //                 PolygonizeTransitionCell (voxelData, origin, cellPos, ref vertices, ref triangleIndices, ref normals, lodIncrementer, transStart);
        //             }
        // }
        return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals.ToArray ());
    }

    private void PolygonizeTransitionCell (IVoxelData volume, Vector3Int offsetPos, Vector3Int cellPos, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int lodIncrementer, Vector3Int transStart) {
        offsetPos += cellPos;
        for (int side = 0; side < 6; side++) {

            // int[] caseCodeCoeffs = new int[9] { 0x01, 0x02, 0x04, 0x80, 0x100, 0x08, 0x40, 0x20, 0x10 };
            float[] halfFaceDensities = new float[9];
            int transCaseCode = 0;
            byte addToCaseCode = 1;
            for (int i = 0; i < 9; i++) {
                Vector3Int pos = offsetPos + transCellFaces[side / 2][i];
                halfFaceDensities[i] = volume[GetTransPosition (pos, side)].density;
                if (halfFaceDensities[i] < isoLevel) {
                    transCaseCode |= addToCaseCode;
                }
                addToCaseCode *= 2;
            }

            if (transCaseCode == 0 || transCaseCode == 511) return;

            byte transCellClass = Tables.TransitionCellClass[transCaseCode];
            Tables.TransitionCell transCell = Tables.TransitionCellData[transCellClass & 0x7F];
            ushort[] vertexData = Tables.TransitionVertexData[transCaseCode];

            byte[] cellIndices = transCell.GetIndices ();
            if (cellIndices == null) return;
            int[] indicesMapping = new int[cellIndices.Length];

            long vertexCount = transCell.GetVertexCount ();
            long triangleCount = transCell.GetTriangleCount ();

            for (int i = 0; i < vertexCount; i++) {
                byte edgeCode = (byte) (vertexData[i]);

                byte cornerA = (byte) (edgeCode & 0x0F);
                byte cornerB = (byte) ((edgeCode & 0xF0) >> 4);

                byte edgeIndex = (byte) ((edgeCode >> 8) & 0x0F);

                Vector3Int cornerAHalfFacePos = GetTransCorner (cornerA, transCellFaces[side / 2]);
                Vector3Int cornerBHalfFacePos = GetTransCorner (cornerB, transCellFaces[side / 2]);

                float densityA = volume[(offsetPos - transStart) + cornerAHalfFacePos].density;
                float densityB = volume[(offsetPos - transStart) + cornerBHalfFacePos].density;

                Vector3Int p0Int = cornerAHalfFacePos;
                Vector3 p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                Vector3Int p1Int = cornerBHalfFacePos;
                Vector3 p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

                float lerpFactor = (isoLevel - densityA) / (densityB - densityA);
                var Q = offsetPos - transStart + (p0 + lerpFactor * (p1 - p0)) / 2;

                indicesMapping[i] = vertices.Count - 1;
                // var QPos = (Q + transStart) / 2;
                vertices.Add (Q *= lodIncrementer);
                normals.Add (GetNormal (offsetPos + cornerAHalfFacePos, offsetPos + cornerBHalfFacePos, volume, lerpFactor));

            }

            bool flipWinding = invertTriangles[side];
            // bool flipWinding = (transCellClass >> 7) != 0;
            for (int t = 0; t < triangleCount; t++) {
                int vertexIndex0 = indicesMapping[cellIndices[t * 3]];
                int vertexIndex1 = indicesMapping[cellIndices[t * 3 + 1]];
                int vertexIndex2 = indicesMapping[cellIndices[t * 3 + 2]];

                Vector3 vertex0 = vertices[vertexIndex0];
                Vector3 vertex1 = vertices[vertexIndex1];
                Vector3 vertex2 = vertices[vertexIndex2];
                if (vertex0 == vertex1 || vertex0 == vertex2 || vertex1 == vertex2) continue; //triangle with zero space

                ushort invert = 1;
                if (flipWinding) invert = 0;

                if ((transCellClass >> 7) % 2 == invert) {
                    triangleIndices.Add (vertexIndex0);
                    triangleIndices.Add (vertexIndex1);
                    triangleIndices.Add (vertexIndex2);
                } else {
                    triangleIndices.Add (vertexIndex0);
                    triangleIndices.Add (vertexIndex2);
                    triangleIndices.Add (vertexIndex1);
                }

            }
        }

    }

    private static Vector3Int GetTransCorner (byte corner, Vector3Int[] transFaceCorners) {
        switch (corner) {
            case 0:
                return transFaceCorners[0];
            case 1:
                return transFaceCorners[1];
            case 2:
                return transFaceCorners[2];
            case 3:
                return transFaceCorners[7];
            case 4:
                return transFaceCorners[8];
            case 5:
                return transFaceCorners[3];
            case 6:
                return transFaceCorners[6];
            case 7:
                return transFaceCorners[5];
            case 8:
                return transFaceCorners[4];
            case 9:
            case 10:
            case 11:
            case 12:
            default:
                return Vector3Int.zero;
        }
    }

    private static Vector3Int GetTransPosition (Vector3Int pos, int side) {
        switch (side) {
            case 0: // x-axis
            case 1:
                return new Vector3Int (0, pos.y, pos.z);
            case 2: // y-axis
            case 3:
                return new Vector3Int (pos.x, 0, pos.z);
            case 4: // z-axis
            case 5:
                return new Vector3Int (pos.x, pos.y, 0);
            default:
                return Vector3Int.zero;
        }
    }

    internal void PolygonizeCell (IVoxelData volume, Vector3Int offsetPos, Vector3Int cellPos, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int lodIncrementer, int lod) {
        offsetPos += cellPos;

        float[] cubeDensities = new float[8];
        byte caseCode = 0;
        byte addToCaseCode = 1;
        for (int i = 0; i < cubeDensities.Length; i++) {
            cubeDensities[i] = volume[offsetPos + Tables.CornerIndex[i] * lodIncrementer].density;
            if (cubeDensities[i] < isoLevel) {
                caseCode |= addToCaseCode;
            }
            addToCaseCode *= 2;
        }

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

            var p0Int = cellPos + Tables.CornerIndex[cornerA] * lodIncrementer;
            var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
            var p1Int = cellPos + Tables.CornerIndex[cornerB] * lodIncrementer;
            var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

            float lerpFactor = (isoLevel - densityA) / (densityB - densityA);
            var Q = p0 + lerpFactor * (p1 - p0);

            normals.Add (GetNormal (offsetPos + Tables.CornerIndex[cornerA] * lodIncrementer, offsetPos + Tables.CornerIndex[cornerB] * lodIncrementer, volume, lerpFactor));
            vertices.Add (Q);
            indicesMapping[i] = vertices.Count - 1;
        }

        for (int t = 0; t < triangleCount; t++) {
            int vertexIndex0 = indicesMapping[cellIndices[t * 3]];
            int vertexIndex1 = indicesMapping[cellIndices[t * 3 + 1]];
            int vertexIndex2 = indicesMapping[cellIndices[t * 3 + 2]];

            Vector3 vertex0 = vertices[vertexIndex0];
            Vector3 vertex1 = vertices[vertexIndex1];
            Vector3 vertex2 = vertices[vertexIndex2];
            if (vertex0 == vertex1 || vertex0 == vertex2 || vertex1 == vertex2) continue; //triangle with zero space

            triangleIndices.Add (vertexIndex0);
            triangleIndices.Add (vertexIndex1);
            triangleIndices.Add (vertexIndex2);
        }
    }

    private Vector3 GetNormal (Vector3Int cornerAPos, Vector3Int cornerBPos, IVoxelData volume, float lerpFactor) {

        float cornerAnx = (volume[cornerAPos + Vector3Int.right].density - volume[cornerAPos - Vector3Int.right].density);
        float cornerAny = (volume[cornerAPos + Vector3Int.up].density - volume[cornerAPos - Vector3Int.up].density);
        float cornerAnz = (volume[cornerAPos + Vector3IntForward].density - volume[cornerAPos - Vector3IntForward].density);

        float cornerBnx = (volume[cornerBPos + Vector3Int.right].density - volume[cornerBPos - Vector3Int.right].density);
        float cornerBny = (volume[cornerBPos + Vector3Int.up].density - volume[cornerBPos - Vector3Int.up].density);
        float cornerBnz = (volume[cornerBPos + Vector3IntForward].density - volume[cornerBPos - Vector3IntForward].density);

        Vector3 normal = new Vector3 (cornerAnx, cornerAny, cornerAnz) * (1f - lerpFactor) + new Vector3 (cornerBnx, cornerBny, cornerBnz) * lerpFactor;
        return -(normal).normalized;

    }
}