using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced : VoxelMeshGenerator {

    public override void Init (MeshGeneratorSettings settings) { }

    public override MeshData GenerateMesh (VoxelChunk chunk) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();

        chunk.voxels.TraverseZYX (delegate (int x, int y, int z, Voxel v) {
            if (x == 0 || y == 0 || z == 0) return;
            if (x > chunk.size || y > chunk.size || z > chunk.size) return;

            Vector3Int cellPos = new Vector3Int (x, y, z);
            // byte validityMask = (byte) ((cellPos.x > 0 ? 1 : 0) | ((cellPos.y > 0 ? 1 : 0) << 2) | ((cellPos.z > 0 ? 1 : 0) << 1));
            float[] cubeDensities = new float[8];
            byte caseCode = 0;
            byte addToCaseCode = 1;
            for (int i = 0; i < cubeDensities.Length; i++) {
                cubeDensities[i] = chunk.voxels.GetVoxel (cellPos + Tables.CornerIndex[i]).density;
                if (cubeDensities[i] < chunk.isoLevel) {
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
                // var cacheReuseIndex = (edgeCode >> 8) & 0xF; //provides the direction of the vertex to be reused (0, 1, 2 or 3)
                // var direction = (edgeCode >> 8) >> 4;

                byte cornerA = (byte) ((edgeCode >> 4) & 0x0F);
                byte cornerB = (byte) (edgeCode & 0x0F);

                float densityA = cubeDensities[cornerA];
                float densityB = cubeDensities[cornerB];

                var p0Int = cellPos + Tables.CornerIndex[cornerA];
                var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                var p1Int = cellPos + Tables.CornerIndex[cornerB];
                var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

                float lerpFactor = (chunk.isoLevel - densityA) / (densityB - densityA);
                var Q = p0 + lerpFactor * (p1 - p0);

                vertices.Add (Q);
                indicesMapping[i] = vertices.Count - 1;
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