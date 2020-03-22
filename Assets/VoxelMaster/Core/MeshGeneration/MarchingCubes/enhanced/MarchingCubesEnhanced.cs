using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced : VoxelMeshGenerator {
    private float isoLevel;

    public override void Init (MeshGeneratorSettings settings) {
        this.isoLevel = settings.isoLevel;
    }

    public override MeshData GenerateMesh (IVoxelData voxelData, Vector3Int origin, int size, int lod) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();
        List<Vector3> normals = new List<Vector3> ();

        for (int z = 0; z < size; z++)
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) {
                    // if (x == 0 || y == 0 || z == 0) continue;
                    // if (x > size || y > size || z > size) continue;

                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (voxelData, origin, cellPos, ref vertices, ref triangleIndices, ref normals, lod);

                }
        return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals.ToArray ());
    }

    private static Vector3Int Vector3IntForward = new Vector3Int (0, 0, 1);

    internal void PolygonizeCell (IVoxelData volume, Vector3Int offsetPos, Vector3Int cellPos, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int lod) {
        offsetPos += cellPos * lod;

        float[] cubeDensities = new float[8];
        byte caseCode = 0;
        byte addToCaseCode = 1;
        for (int i = 0; i < cubeDensities.Length; i++) {
            cubeDensities[i] = volume[offsetPos + Tables.CornerIndex[i]].density;
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

            int cornerA = (byte) ((edgeCode >> 4) & 0x0F);
            byte cornerB = (byte) (edgeCode & 0x0F);

            float densityA = cubeDensities[cornerA];
            float densityB = cubeDensities[cornerB];

            var p0Int = cellPos + Tables.CornerIndex[cornerA];
            var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
            var p1Int = cellPos + Tables.CornerIndex[cornerB];
            var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

            float lerpFactor = (isoLevel - densityA) / (densityB - densityA);
            var Q = p0 + lerpFactor * (p1 - p0);

            normals.Add (GetNormal (offsetPos + Tables.CornerIndex[cornerA], offsetPos + Tables.CornerIndex[cornerB], volume, lerpFactor));
            vertices.Add (Q);
            indicesMapping[i] = vertices.Count - 1;
        }
        for (int t = 0; t < triangleCount; t++) {
            for (int i = 0; i < 3; i++) {
                triangleIndices.Add (indicesMapping[cellIndices[t * 3 + i]]);
            }
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