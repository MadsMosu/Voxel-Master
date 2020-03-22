using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced : VoxelMeshGenerator {
    private float isoLevel;

    public override void Init (MeshGeneratorSettings settings) {
        this.isoLevel = settings.isoLevel;
    }

    public override MeshData GenerateMesh (IVoxelData voxelData, Vector3Int origin, int size, int lod) {
        int numCells = size * size * size;
        List<Vector3> vertices = new List<Vector3> (numCells * 12);
        List<int> triangleIndices = new List<int> (numCells * 12);
        List<Vector3> normals = new List<Vector3> (numCells * 12);

        int incrementer = lod == 0 ? 1 : lod * 2;

        for (int z = 0; z < size; z += incrementer)
            for (int y = 0; y < size; y += incrementer)
                for (int x = 0; x < size; x += incrementer) {
                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (voxelData, origin, cellPos, ref vertices, ref triangleIndices, ref normals, incrementer);
                }
        return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals.ToArray ());
    }

    private static Vector3Int Vector3IntForward = new Vector3Int (0, 0, 1);

    internal void PolygonizeCell (IVoxelData volume, Vector3Int offsetPos, Vector3Int cellPos, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int lod) {
        offsetPos += cellPos;

        float[] cubeDensities = new float[8];
        byte caseCode = 0;
        byte addToCaseCode = 1;
        for (int i = 0; i < cubeDensities.Length; i++) {
            cubeDensities[i] = volume[offsetPos + Tables.CornerIndex[i] * lod].density;
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

            var p0Int = cellPos + Tables.CornerIndex[cornerA] * lod;
            var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
            var p1Int = cellPos + Tables.CornerIndex[cornerB] * lod;
            var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

            float lerpFactor = (isoLevel - densityA) / (densityB - densityA);
            var Q = p0 + lerpFactor * (p1 - p0);

            normals.Add (GetNormal (offsetPos + Tables.CornerIndex[cornerA] * lod, offsetPos + Tables.CornerIndex[cornerB] * lod, volume, lerpFactor));
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
        return (normal);

    }
}