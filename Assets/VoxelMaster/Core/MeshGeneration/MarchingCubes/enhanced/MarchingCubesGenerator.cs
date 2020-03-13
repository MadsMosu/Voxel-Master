using System;
using System.Collections.Generic;
using UnityEngine;

//BASED ON https://github.com/BinaryConstruct/Transvoxel-XNA/blob/139b3cff9457050c109bd2351d50188e863a4a9a/Projects/Transvoxel/SurfaceExtractor/SurfaceExtractor.cs
public class MarchingCubesEnhanced : VoxelMeshGenerator {

    private RegularCellCache cache;

    public override void Init (MeshGeneratorSettings settings) {
        this.cache = RegularCellCache.Cache (settings.chunkSize);
    }

    public override MeshData GenerateMesh (VoxelChunk chunk) {
        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangleIndices = new List<int> ();
        List<Vector3> normals = new List<Vector3> ();

        chunk.voxels.Traverse (delegate (int x, int y, int z, Voxel v) {
            if (x == 0 || y == 0 || z == 0) return;
            if (x > chunk.size || y > chunk.size || z > chunk.size) return;

            Vector3Int cellPos = new Vector3Int (x, y, z) * chunk.lod;
            sbyte[] cubeDensities = new sbyte[8];
            for (int i = 0; i < cubeDensities.Length; i++) {
                cubeDensities[i] = chunk.voxels.GetVoxel (cellPos + Tables.CornerIndex[i]).density;
            }

            byte caseCode = (byte) (
                ((cubeDensities[0] >> 7) & 1) |
                ((cubeDensities[1] >> 6) & 2) |
                ((cubeDensities[2] >> 5) & 4) |
                ((cubeDensities[3] >> 4) & 8) |
                ((cubeDensities[4] >> 3) & 16) |
                ((cubeDensities[5] >> 2) & 32) |
                ((cubeDensities[6] >> 1) & 64) |
                (cubeDensities[7] & 128)
            );

            //if casecode is either 0 or 255 there is no surface
            if ((caseCode ^ ((cubeDensities[7] >> 7) & 255)) == 0) return;

            byte regularCellClass = Tables.RegularCellClass[caseCode]; //get the equivalence class based on the casecode
            ushort[] vertexPositions = Tables.RegularVertexData[caseCode]; //get all possible vertex positions

            Tables.RegularCell regularCell = Tables.RegularCellData[regularCellClass]; //get the regular cell that matches the equivalence class
            long vertexCount = regularCell.GetVertexCount (); //the number of vertices that fits the class. Max 12 vertices
            long triangleCount = regularCell.GetTriangleCount (); //the number of triangles that fits the class. Max 5 triangles
            byte[] indexOffset = regularCell.Indizes (); //the vertex sequences for the cell
            ushort[] mappedIndizes = new ushort[indexOffset.Length];

            for (int i = 0; i < vertexCount; i++) {
                byte edge = (byte) (vertexPositions[i] >> 8);
                byte reuseIndex = (byte) (edge & 15);
                byte rDir = (byte) (edge >> 4); //the direction to go to reach a previous cell for reusing

                byte v0 = (byte) ((vertexPositions[i] >> 4) & 15); //corner a of edge
                byte v1 = (byte) ((vertexPositions[i]) & 15); //corner b of edge

                int vertexIndex = -1;
                byte directionMask = (byte) ((cellPos.x > 0 ? 1 : 0) | ((cellPos.z > 0 ? 1 : 0) << 1) | ((cellPos.y > 0 ? 1 : 0) << 2)); //3-bit direction code that indicates the direction to go in to reach the preceding cell

                //reuse cell
                if (v1 != 7 && (rDir & directionMask) == rDir) {
                    ReuseCell reuseCell = cache.GetReusedIndex (cellPos, rDir);
                    vertexIndex = reuseCell.vertexIndices[reuseIndex];
                }
                //generate vertex
                if (vertexIndex == -1) {
                    sbyte d0 = cubeDensities[v0];
                    sbyte d1 = cubeDensities[v1];

                    long t = (d1 << 8) / (d1 - d0);

                    Vector3Int p0Int = (cellPos + Tables.CornerIndex[v0] * chunk.lod) * chunk.lod;
                    Vector3 p0 = new Vector3 (p0Int.x, p0Int.y, p0Int.z);
                    Vector3Int p1Int = (cellPos + Tables.CornerIndex[v1] * chunk.lod) * chunk.lod;
                    Vector3 p1 = new Vector3 (p1Int.x, p1Int.y, p1Int.z);

                    // RemoveSurfaceShifting (chunk, chunk.lod, ref d0, ref d1, ref t, ref p0Int, ref p0, ref p1Int, ref p1);
                    Vector3 vertex = LinearInterp (t, p0, p1);

                    vertices.Add (vertex);

                    // Vector3 n0 = GetNormal (cellPos, cubeDensities[v0], chunk);
                    // Vector3 n1 = GetNormal (cellPos, cubeDensities[v1], chunk);
                    // Vector3 normal = LinearInterp (t, n0, n1);

                    // normals.Add (normal);
                    vertexIndex = vertices.Count - 1;
                }

                if ((rDir & 8) != 0) {
                    //set the previous vertex index as a reusable index
                    cache.SetReusableIndex (cellPos, reuseIndex, (ushort) (vertices.Count - 1));
                }

                mappedIndizes[i] = (ushort) vertexIndex;
            }
            //generate triangle indices
            for (int t = 0; t < triangleCount; t++) {
                for (int i = 0; i < 3; i++) {
                    triangleIndices.Add (mappedIndizes[regularCell.Indizes () [t * 3 + i]]);
                }
            }

        });
        return new MeshData (vertices.ToArray (), triangleIndices.ToArray ());
    }

    private void RemoveSurfaceShifting (VoxelChunk chunk, int lod, ref sbyte d0, ref sbyte d1, ref long t, ref Vector3Int p0Int, ref Vector3 p0, ref Vector3Int p1Int, ref Vector3 p1) {
        for (int i = 0; i < lod; i++) {
            Vector3 vm = (p0 + p1) / 2.0f;
            Vector3Int pm = (p0Int + p1Int) / 2;
            sbyte density = chunk.voxels.GetVoxel (pm).density;

            if ((d0 & 143) != (d1 & 143)) {
                p1 = vm;
                p1Int = pm;
                d1 = density;
            } else {
                p0 = vm;
                p0Int = pm;
                d0 = density;
            }
        }
        t = (d1 << 8) / (d1 - d0);
    }

    private Vector3 LinearInterp (long t, Vector3 p0, Vector3 p1) {
        long u = 256 - t;
        float s = 1.0f / 256.0f;
        Vector3 Q = p0 * t + p1 * u;
        Q *= s;
        return Q;
    }

    private Vector3 GetNormal (Vector3Int cellPos, byte cornerIndex, VoxelChunk chunk) {
        Vector3Int corner = Tables.CornerIndex[cornerIndex];
        sbyte dxa = chunk.voxels.GetVoxel (new Vector3Int (cellPos.x + corner.x + 1, cellPos.y + corner.y, cellPos.z + corner.z)).density;
        sbyte dxb = chunk.voxels.GetVoxel (new Vector3Int (cellPos.x + corner.x - 1, cellPos.y + corner.y, cellPos.z + corner.z)).density;

        sbyte dya = chunk.voxels.GetVoxel (new Vector3Int (cellPos.x + corner.x, cellPos.y + corner.y + 1, cellPos.z + corner.z)).density;
        sbyte dyb = chunk.voxels.GetVoxel (new Vector3Int (cellPos.x + corner.x, cellPos.y + corner.y - 1, cellPos.z + corner.z)).density;

        sbyte dza = chunk.voxels.GetVoxel (new Vector3Int (cellPos.x + corner.x, cellPos.y + corner.y, cellPos.z + corner.z + 1)).density;
        sbyte dzb = chunk.voxels.GetVoxel (new Vector3Int (cellPos.x + corner.x, cellPos.y + corner.y, cellPos.z + corner.z - 1)).density;

        var gradient = new Vector3 (dxa - dxb, dya - dyb, dza - dzb);
        gradient.Normalize ();
        return gradient;
    }
}