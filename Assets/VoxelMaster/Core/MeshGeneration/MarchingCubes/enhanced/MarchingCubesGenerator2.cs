using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced2 : VoxelMeshGenerator {

    public override void Init (MeshGeneratorSettings settings) { }

    public override MeshData GenerateMesh (VoxelChunk chunk) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();

        chunk.voxels.TraverseZYX (delegate (int x, int y, int z, Voxel v) {
            if (x == 0 || y == 0 || z == 0) return;
            if (x > chunk.size || y > chunk.size || z > chunk.size) return;

            Vector3Int cellPos = new Vector3Int (x, y, z);
            byte validityMask = (byte) ((cellPos.x > 0 ? 1 : 0) | ((cellPos.y > 0 ? 1 : 0) << 2) | ((cellPos.z > 0 ? 1 : 0) << 1));

            sbyte[] cubeDensities = new sbyte[8];
            for (int i = 0; i < cubeDensities.Length; i++) {
                cubeDensities[i] = chunk.voxels.GetVoxel (cellPos + Tables.CornerIndex[i]).density;
            }

            byte caseCode = (byte) (
                ((cubeDensities[0] >> 7) & 0x01) |
                ((cubeDensities[1] >> 6) & 0x02) |
                ((cubeDensities[2] >> 5) & 0x04) |
                ((cubeDensities[3] >> 4) & 0x08) |
                ((cubeDensities[4] >> 3) & 0x10) |
                ((cubeDensities[5] >> 2) & 0x20) |
                ((cubeDensities[6] >> 1) & 0x40) |
                (cubeDensities[7] & 0x80)
            );

            //if casecode is either 0 or 255 there is no surface
            if ((caseCode ^ ((cubeDensities[7] >> 7) & 0xFF)) == 0) return;

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
                // var cacheReuseIndex = (edgeCode >> 8) & 0xF; //provides the direction of the vertex to be reused (0, 1, 2 or 3)
                // var direction = (edgeCode >> 8) >> 4;

                byte cornerA = (byte) ((edgeCode >> 4) & 0x0F);
                byte cornerB = (byte) (edgeCode & 0x0F);

                long densityA = cubeDensities[cornerA];
                long densityB = cubeDensities[cornerB];

                long lerpFactor = (densityB << 8) / (densityB - densityA);
                long inverseLerpFactor = 0x0100 - lerpFactor;
                var p0Int = cellPos + Tables.CornerIndex[cornerA];
                var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                var p1Int = cellPos + Tables.CornerIndex[cornerB];
                var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

                var Q = (lerpFactor * p0 + inverseLerpFactor * p1);

                vertices.Add (Q);
                indicesMapping[i] = vertices.Count - 1;
                // if ((lerpFactor & 0x00FF) != 0) {
                //     if (cornerB == 7) {
                //         long inverseLerpFactor = 0x0100 - lerpFactor;
                //         var p0Int = cellPos + Tables.CornerIndex[cornerA];
                //         var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                //         var p1Int = cellPos + Tables.CornerIndex[cornerB];
                //         var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

                //         var Q = (lerpFactor * p0 + inverseLerpFactor * p1) * (1.0f / 256.0f);
                //         vertices.Add (Q);
                //         MapVertice (i, vertices.Count - 1, indicesMapping, cellIndices);
                //         SetVertexCacheIndex (cellPos, cacheReuseIndex, (short) (vertices.Count - 1), chunk);
                //     } else {
                //         if ((direction & validityMask) == direction) {
                //             var reusedVertexIndex = GetReusedVertexCache (cellPos, direction, chunk) [cacheReuseIndex];
                //             MapVertice (i, reusedVertexIndex, indicesMapping, cellIndices);
                //         } else {
                //             long inverseLerpFactor = 0x0100 - lerpFactor;
                //             var p0Int = cellPos + Tables.CornerIndex[cornerA];
                //             var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                //             var p1Int = cellPos + Tables.CornerIndex[cornerB];
                //             var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

                //             var Q = (lerpFactor * p0 + inverseLerpFactor * p1) * (1.0f / 256.0f);
                //             vertices.Add (Q);
                //             MapVertice (i, vertices.Count - 1, indicesMapping, cellIndices);
                //             SetVertexCacheIndex (cellPos, cacheReuseIndex, (short) (vertices.Count - 1), chunk);
                //         }
                //     }

                // } else if (lerpFactor == 0) {
                //     if (cornerB == 7) {
                //         var p0Int = cellPos + Tables.CornerIndex[cornerB];
                //         var Q = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                //         vertices.Add (Q);
                //         MapVertice (i, vertices.Count - 1, indicesMapping, cellIndices);
                //         SetVertexCacheIndex (cellPos, 0, (short) (vertices.Count - 1), chunk);
                //     } else {
                //         //reuse vertex
                //         direction = cornerB ^ 7;
                //         if ((direction & validityMask) == direction) {
                //             var reusedVertexIndex = GetReusedVertexCache (cellPos, direction, chunk) [0];
                //             MapVertice (i, reusedVertexIndex, indicesMapping, cellIndices);
                //         } else {
                //             var p0Int = cellPos + Tables.CornerIndex[cornerA];
                //             var Q = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                //             vertices.Add (Q);
                //             MapVertice (i, vertices.Count - 1, indicesMapping, cellIndices);
                //             SetVertexCacheIndex (cellPos, 0, (short) (vertices.Count - 1), chunk);
                //         }
                //     }
                // } else {
                //     direction = cornerA ^ 7;
                //     if ((direction & validityMask) == direction) {
                //         //reuse vertex
                //         var reusedVertexIndex = GetReusedVertexCache (cellPos, direction, chunk) [0];
                //         if (reusedVertexIndex == -1) {
                //             //reusable vertex does not exist
                //             var p0Int = cellPos + Tables.CornerIndex[cornerA];
                //             var Q = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                //             vertices.Add (Q);
                //             MapVertice (i, vertices.Count - 1, indicesMapping, cellIndices);
                //             SetVertexCacheIndex (cellPos, 0, (short) (vertices.Count - 1), chunk);
                //         } else {
                //             MapVertice (i, reusedVertexIndex, indicesMapping, cellIndices);
                //         }
                //     } else {
                //         var p0Int = cellPos + Tables.CornerIndex[cornerA];
                //         var Q = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                //         vertices.Add (Q);
                //         MapVertice (i, vertices.Count - 1, indicesMapping, cellIndices);
                //         SetVertexCacheIndex (cellPos, 0, (short) (vertices.Count - 1), chunk);
                //     }
                // }
            }
            for (int t = 0; t < triangleCount; t++) {
                for (int i = 0; i < 3; i++) {
                    triangleIndices.Add (indicesMapping[cellIndices[t * 3 + i]]);
                }
            }
        });

        return new MeshData (vertices.ToArray (), triangleIndices.ToArray ());
    }

    private short[] GetReusedVertexCache (Vector3Int v, int direction, VoxelChunk chunk) {
        var reuseX = (direction) & 0x1;
        var reuseY = (direction >> 2) & 0x1;
        var reuseZ = (direction >> 1) & 0x1;

        Vector3Int pos = v - new Vector3Int (reuseX, reuseY, reuseZ);
        return chunk.reuseVertexCache[pos.z & 1][pos.y * chunk.size + pos.x];
    }

    private void SetVertexCacheIndex (Vector3Int pos, int reuseCacheIndex, short value, VoxelChunk chunk) {
        chunk.reuseVertexCache[pos.z & 1][pos.y * chunk.size + pos.x][reuseCacheIndex] = value;
    }

    private void MapVertice (int i, int reusedVertexIndex, int[] indicesMapping, byte[] cellIndices) {
        for (int j = 0; j < indicesMapping.Length; j++) {
            if (i == cellIndices[j]) {
                indicesMapping[j] = reusedVertexIndex;
            }
        }
    }
}