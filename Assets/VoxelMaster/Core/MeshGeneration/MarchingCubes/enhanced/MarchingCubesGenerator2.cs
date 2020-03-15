using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubesEnhanced2 : VoxelMeshGenerator {

    public override void Init (MeshGeneratorSettings settings) { }

    public override MeshData GenerateMesh (IVoxelData voxelData, Vector3Int origin, int size) {

        List<Vector3> vertices = new List<Vector3> ();
        List<int> triangles = new List<int> ();
        List<Vector3> normals = new List<Vector3> ();

        for (int z = 0; z < size; z++)
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) {
                    if (x == 0 || y == 0 || z == 0) continue;
                    if (x > size || y > size || z > size) continue;

                    Vector3Int cellPos = new Vector3Int (x, y, z);
                    PolygonizeCell (voxelData, origin, cellPos, ref vertices, ref triangles, ref normals, 1);
                }

        return new MeshData (vertices.ToArray (), triangles.ToArray (), normals.ToArray ());
    }

    private static Vector3Int Vector3IntForward = new Vector3Int (0, 0, 1);

    internal void PolygonizeCell (IVoxelData volume, Vector3Int offsetPos, Vector3Int pos, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, int lod) {
        Debug.Assert (lod >= 1, "Level of Detail must be greater than 1");
        offsetPos += pos * lod;

        byte directionMask = (byte) ((pos.x > 0 ? 1 : 0) | ((pos.z > 0 ? 1 : 0) << 1) | ((pos.y > 0 ? 1 : 0) << 2));

        sbyte[] density = new sbyte[8];

        for (int i = 0; i < density.Length; i++) {
            density[i] = volume[offsetPos + Tables.CornerIndex[i] * lod].density;
        }

        byte caseCode = getCaseCode (density);
        if ((caseCode ^ ((density[7] >> 7) & 0xFF)) == 0) //for this cases there is no triangulation
            return;

        Vector3[] cornerNormals = new Vector3[8];
        for (int i = 0; i < 8; i++) {
            var p = offsetPos + Tables.CornerIndex[i] * lod;
            float nx = (volume[p + Vector3Int.right].density - volume[p - Vector3Int.right].density) * 0.5f;
            float ny = (volume[p + Vector3Int.up].density - volume[p - Vector3Int.up].density) * 0.5f;
            float nz = (volume[p + Vector3IntForward].density - volume[p - Vector3IntForward].density) * 0.5f;

            cornerNormals[i].x = nx;
            cornerNormals[i].y = ny;
            cornerNormals[i].z = nz;
            cornerNormals[i].Normalize ();
        }

        byte regularCellClass = Tables.RegularCellClass[caseCode];
        ushort[] vertexLocations = Tables.RegularVertexData[caseCode];

        Tables.RegularCell c = Tables.RegularCellData[regularCellClass];
        long vertexCount = c.GetVertexCount ();
        long triangleCount = c.GetTriangleCount ();
        byte[] indexOffset = c.GetIndices (); //index offsets for current cell
        ushort[] mappedIndizes = new ushort[indexOffset.Length]; //array with real indizes for current cell

        for (int i = 0; i < vertexCount; i++) {
            byte edge = (byte) (vertexLocations[i] >> 8);
            byte reuseIndex = (byte) (edge & 0xF); //Vertex id which should be created or reused 1,2 or 3
            byte rDir = (byte) (edge >> 4); //the direction to go to reach a previous cell for reusing 

            byte v1 = (byte) ((vertexLocations[i]) & 0x0F); //Second Corner Index
            byte v0 = (byte) ((vertexLocations[i] >> 4) & 0x0F); //First Corner Index

            sbyte d0 = density[v0];
            sbyte d1 = density[v1];

            Debug.Assert (v1 > v0);

            int t = (d1 << 8) / (d1 - d0);
            int u = 0x0100 - t;
            float t0 = t / 256f;
            float t1 = u / 256f;

            Vector3 normal = cornerNormals[v0] * t0 + cornerNormals[v1] * t1;
            normals.Add (normal);

            Vector3 vertex = GenerateVertex (ref offsetPos, ref pos, lod, t, ref v0, ref v1, ref d0, ref d1);
            vertices.Add (vertex);

            mappedIndizes[i] = (ushort) (vertices.Count - 1);
        }

        for (int t = 0; t < triangleCount; t++) {
            for (int i = 0; i < 3; i++) {
                triangles.Add (mappedIndizes[c.GetIndices () [t * 3 + i]]);
            }
        }
    }

    private static byte getCaseCode (sbyte[] density) {
        byte code = 0;
        byte konj = 0x01;
        for (int i = 0; i < density.Length; i++) {
            code |= (byte) ((density[i] >> (density.Length - 1 - i)) & konj);
            konj <<= 1;
        }

        return code;
    }

    private Vector3 GenerateVertex (ref Vector3Int offsetPos, ref Vector3Int pos, int lod, long t, ref byte v0, ref byte v1, ref sbyte d0, ref sbyte d1) {
        Vector3Int iP0 = (pos + Tables.CornerIndex[v0] * lod);
        Vector3 P0; // = new Vector3f(iP0.x, iP0.y, iP0.z);
        P0.x = iP0.x;
        P0.y = iP0.y;
        P0.z = iP0.z;

        Vector3Int iP1 = (pos + Tables.CornerIndex[v1] * lod);
        Vector3 P1; // = new Vector3f(iP1.x, iP1.y, iP1.z);
        P1.x = iP1.x;
        P1.y = iP1.y;
        P1.z = iP1.z;

        Vector3 Q = InterpolateVoxelVector (t, P0, P1);

        return Q;
    }

    internal static Vector3 InterpolateVoxelVector (long t, Vector3 P0, Vector3 P1) {
        long u = 0x0100 - t; //256 - t
        float s = 1.0f / 256.0f;
        Vector3 Q = P0 * t + P1 * u; //Density Interpolation
        Q *= s; // shift to shader ! 
        return Q;
    }

}