using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced : VoxelMeshGenerator {
    private float isoLevel;
    private static Vector3Int[][] toIterate;

    public override void Init (MeshGeneratorSettings settings) {
        this.isoLevel = settings.isoLevel;

        var chunkLodStart = settings.chunkSize - 2;
        var chunkLodEnd = settings.chunkSize - 1;
        toIterate = new Vector3Int[][] {
            new Vector3Int[] { new Vector3Int (0, 0, 0), new Vector3Int (1, chunkLodStart, chunkLodStart) },
            new Vector3Int[] { new Vector3Int (chunkLodStart, 0, 0), new Vector3Int (chunkLodEnd, chunkLodStart, chunkLodStart) },
            new Vector3Int[] { new Vector3Int (0, 0, 0), new Vector3Int (chunkLodStart, 1, chunkLodStart) },
            new Vector3Int[] { new Vector3Int (0, chunkLodStart, 0), new Vector3Int (chunkLodStart, chunkLodEnd, chunkLodStart) },
            new Vector3Int[] { new Vector3Int (0, 0, 0), new Vector3Int (chunkLodStart, chunkLodStart, 1) },
            new Vector3Int[] { new Vector3Int (0, 0, chunkLodStart), new Vector3Int (chunkLodStart, chunkLodStart, chunkLodEnd) }
        };
    }

    public override MeshData GenerateMesh (IVoxelData voxelData, Vector3Int origin, int size, int lod) {
        int numCells = size * size * size;
        List<Vector3> vertices = new List<Vector3> (numCells * 12);
        List<int> triangleIndices = new List<int> (numCells * 12);
        List<Vector3> normals = new List<Vector3> (numCells * 12);

        for (int z = 0; z < size - 1; z++)
            for (int y = 0; y < size - 1; y++)
                for (int x = 0; x < size - 1; x++) {
                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (voxelData, origin, cellPos, ref vertices, ref triangleIndices, ref normals, lod);
                }

        if (lod > 0) {

        }
        return new MeshData (vertices.ToArray (), triangleIndices.ToArray (), normals.ToArray ());
    }

    internal void PolygonizeTransitionCell (IVoxelData volume, Vector3Int cellPos, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int lod, Vector3Int start, Vector3Int end, bool invertTris, int lodFactor) {

    }

    internal void PolygonizeCell (IVoxelData volume, Vector3Int offsetPos, Vector3Int cellPos, ref List<Vector3> vertices, ref List<int> triangleIndices, ref List<Vector3> normals, int lod) {
        offsetPos += cellPos;

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

            Vector3 triangleSurfaceNormal = Vector3.Cross ((vertex1 - vertex0), (vertex2 - vertex0)).normalized;
            Vector3 vertex0Normal = triangleSurfaceNormal;
            Vector3 vertex1Normal = triangleSurfaceNormal;
            Vector3 vertex2Normal = triangleSurfaceNormal;
        }

    }

    //DONT DELETE BELOW - MIGHT BE USED FOR LATER WHEN IMPLEMENTING CACHING

    // private short[] GetReusedVertexCache (Vector3Int v, int direction, VoxelChunk chunk) {
    //     var reuseX = (direction) & 0x1;
    //     var reuseY = (direction >> 2) & 0x1;
    //     var reuseZ = (direction >> 1) & 0x1;

    //     Vector3Int pos = v - new Vector3Int (reuseX, reuseY, reuseZ);
    //     return chunk.reuseVertexCache[pos.z & 1][pos.y * chunk.size + pos.x];
    // }

    // private void SetVertexCacheIndex (Vector3Int pos, int reuseCacheIndex, short value, VoxelChunk chunk) {
    //     chunk.reuseVertexCache[pos.z & 1][pos.y * chunk.size + pos.x][reuseCacheIndex] = value;
    // }

    // private void MapVertice (int i, int reusedVertexIndex, int[] indicesMapping, byte[] cellIndices) {
    //     for (int j = 0; j < indicesMapping.Length; j++) {
    //         if (i == cellIndices[j]) {
    //             indicesMapping[j] = reusedVertexIndex;
    //         }
    //     }
    // }
}