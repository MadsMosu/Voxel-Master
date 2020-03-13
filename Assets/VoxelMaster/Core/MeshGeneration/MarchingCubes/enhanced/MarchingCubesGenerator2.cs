using System.Collections.Generic;
using UnityEngine;

public class MarchingCubesEnhanced2 : VoxelMeshGenerator {

    public override void Init (MeshGeneratorSettings settings) {
        throw new System.NotImplementedException ();
    }
    public override MeshData GenerateMesh (VoxelChunk chunk) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();

        chunk.voxels.TraverseZYX (delegate (int x, int y, int z, Voxel v) {

            Vector3Int cellPos = new Vector3Int (x, y, z);
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
            var cache = new int[4] {-1, -1, -1, -1 };

            byte regularCellClass = Tables.RegularCellClass[caseCode];
            Tables.RegularCell regularCell = Tables.RegularCellData[regularCellClass];
            var vertexData = Tables.RegularVertexData[caseCode];
            int[] indicesMapping = new int[regularCell.GetIndices ().Length]; //maps the added indices to the vertexData indices

            long vertexCount = regularCell.GetVertexCount ();
            for (int i = 0; i < vertexCount; i++) {
                ushort edgeCode = vertexData[i];
                var cacheReuseIndex = (edgeCode >> 8) & 0xF; //provides the direction of the vertex to be reused (0, 1, 2 or 3)

                int cornerA = (edgeCode >> 4) & 0x0F;
                int cornerB = edgeCode & 0x0F;

                sbyte densityA = cubeDensities[cornerA];
                sbyte densityB = cubeDensities[cornerB];

                long lerpFactor = (densityB << 8) / (densityB - densityA);
                if ((lerpFactor & 0x00FF) != 0) {
                    long inverseLerpFactor = 0x0100 - lerpFactor;
                    var p0Int = cellPos + Tables.CornerIndex[cornerA];
                    var p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                    var p1Int = cellPos + Tables.CornerIndex[cornerB];
                    var p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

                    var Q = lerpFactor * p0 + inverseLerpFactor * p1;
                    vertices.Add (Q);
                    indicesMapping[i] = vertices.Count - 1;
                    cache[cacheReuseIndex] = vertices.Count - 1;

                } else if (lerpFactor == 0) {
                    if (cornerB == 7) {
                        var p0Int = cellPos + Tables.CornerIndex[cornerB];
                        var Q = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                        vertices.Add (Q);
                        indicesMapping[i] = vertices.Count - 1;
                        cache[cacheReuseIndex] = vertices.Count - 1;
                    } else {
                        //reuse vertex
                        var reuse = cornerA ^ 7;

                        var reuseX = (reuse >> 2) & 0x1;
                        var reuseY = (reuse >> 1) & 0x1;
                        var reuseZ = (reuse >> 0) & 0x1;

                        var pos = (cellPos + Tables.CornerIndex[cornerA]) - new Vector3Int (reuseX, reuseY, reuseZ);
                        //cornerA must be 7 since cornerB isn't 7, hence we store it at index 0 in the cache
                        if (cache[0] == -1) {
                            //add new vertex index to the cache
                            cache[0] = vertices.Count - 1;
                        } else {
                            //get vertex index
                            //add index
                            indicesMapping[i] = cache[0];
                        }
                    }
                } else {
                    //reuse vertex
                }
            }

        });

        return new MeshData (new Vector3[0], new int[0]);
    }
}