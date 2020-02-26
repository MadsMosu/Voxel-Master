using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DualContouring : VoxelMeshGenerator {

    private readonly static Vector2Int[] edges = new Vector2Int[12] {
        new Vector2Int (0, 1), new Vector2Int (0, 2), new Vector2Int (0, 4), new Vector2Int (1, 3), //x-axis
        new Vector2Int (1, 5), new Vector2Int (2, 3), new Vector2Int (2, 6), new Vector2Int (4, 5), //y-axis
        new Vector2Int (4, 6), new Vector2Int (3, 7), new Vector2Int (6, 7), new Vector2Int (5, 7) //z-axis
    };

    private readonly static Vector3Int[] vertOffsets = new Vector3Int[8] {
        new Vector3Int (0, 0, 0),
        new Vector3Int (0, 0, 1),
        new Vector3Int (0, 1, 0),
        new Vector3Int (0, 1, 1),
        new Vector3Int (1, 0, 0),
        new Vector3Int (1, 0, 1),
        new Vector3Int (1, 1, 0),
        new Vector3Int (1, 1, 1),
    };

    private readonly static Vector3Int[, ] directions = new Vector3Int[3, 3] { { new Vector3Int (1, 0, 0), new Vector3Int (1, 0, 1), new Vector3Int (0, 0, 1) }, { new Vector3Int (0, 1, 0), new Vector3Int (1, 1, 0), new Vector3Int (1, 0, 0) }, { new Vector3Int (0, 0, 1), new Vector3Int (0, 1, 1), new Vector3Int (0, 1, 0) }
    };

    private Vector3 Lerp (Vector3 v0, Vector3 v1, float t) {
        return (1 - t) * v0 + t * v1;
    }

    private float InverseLerp (float a, float b, float t) {
        if (a != b) {
            return (t - a / (b - a));
        }
        return 0;
    }

    private List<Vector3> normals;
    private List<Vector3> vertices;
    private List<int> triangleIndicies;
    private Vector3[] cellPoints;

    public override MeshData GenerateMesh (VoxelChunk chunk, Func<Vector3, float> densityFunction) {
        vertices = new List<Vector3> ();
        triangleIndicies = new List<int> ();
        normals = new List<Vector3> ();
        cellPoints = new Vector3[chunk.size.x * chunk.size.y * chunk.size.z];

        //for each cube that exhibits a sign change, a vertex is generated positioned at the minimizer of the QEF
        chunk.voxels.Traverse (delegate (int x, int y, int z, Voxel v) {
            GenerateCellVertex (new Vector3Int (x, y, z), chunk, densityFunction);
        });

        // for each of the edges that went through a sign change, triangles are generated connecting the vertices of the four adjacent cells
        chunk.voxels.Traverse (delegate (int x, int y, int z, Voxel v) {
            ConnectCellVertices (new Vector3Int (x, y, z), chunk);
        });

        for (int i = 0; i < vertices.Count; i += 4) {
            triangleIndicies.Add (i);
            triangleIndicies.Add (i + 1);
            triangleIndicies.Add (i + 2);

            triangleIndicies.Add (i + 2);
            triangleIndicies.Add (i + 3);
            triangleIndicies.Add (i);
        }
        return new MeshData (vertices.ToArray (), triangleIndicies.ToArray ());
    }

    private void GenerateCellVertex (Vector3Int cellPos, VoxelChunk chunk, Func<Vector3, float> densityFunction) {
        if (cellPos.x + 1 >= chunk.size.x || cellPos.y + 1 >= chunk.size.y || cellPos.z + 1 >= chunk.size.z) return;

        int corners = 0;
        for (int i = 0; i < 8; i++) {
            float density = chunk.voxels.GetVoxel (cellPos + vertOffsets[i]).density;
            if (density < chunk.isoLevel) {
                corners |= 1 << i;
            }
        }
        // if all of the corners are either outside or inside of surface boundary
        if (corners == 0 || corners == 255) return;

        QEF3D qef = new QEF3D ();
        Vector3 averageNormal = new Vector3 ();
        for (int i = 0; i < edges.Length; i++) {
            Vector2Int edge = edges[i];
            int m1 = (corners >> edge.x) & 0b1;
            int m2 = (corners >> edge.y) & 0b1;

            // check for zero crossing point. the point of where the sign function changes (eg. positive to negative or negative to positive)
            // only continue if there is no zero crossing (position of the surface). no need to make vertex inside the cube when there is no surface border
            if (m1 == m2) continue;

            Vector3Int aPos = cellPos + vertOffsets[edge.x];
            Vector3Int bPos = cellPos + vertOffsets[edge.y];

            Vector3 aOffset = vertOffsets[edge.x];
            Vector3 bOffset = vertOffsets[edge.y];

            float aDensity = chunk.voxels.GetVoxel (aPos).density;
            float bDensity = chunk.voxels.GetVoxel (bPos).density;

            var intersectionPoint = Lerp (aPos, bPos, InverseLerp (aDensity, bDensity, chunk.isoLevel));
            // var intersectionPoint = (aOffset + (-aDensity) * (bOffset - aOffset) / (bDensity - aDensity));

            var normal = GetNormal (intersectionPoint, densityFunction);
            averageNormal += normal;

            qef.Add (intersectionPoint, normal);
        }
        //the vertex that is clostest to the surface
        var vertex = qef.Solve ();
        var averagedNormal = Vector3.Normalize (averageNormal / (float) qef.Intersections.Count);
        Vector3 c_v = averagedNormal * 0.5f + Vector3.one * 0.5f;
        c_v.Normalize ();
        normals.Add (averagedNormal);

        cellPoints[Util.Map3DTo1D (cellPos, chunk.size)] = vertex;
    }

    private void ConnectCellVertices (Vector3Int cellPos, VoxelChunk chunk) {
        if (cellPos.x + 1 >= chunk.size.x || cellPos.y + 1 >= chunk.size.y || cellPos.z + 1 >= chunk.size.z) return;
        var v0 = cellPos;

        for (int i = 0; i < 3; i++) {
            var v1 = v0 + directions[i, 0];
            var v2 = v0 + directions[i, 1];
            var v3 = v0 + directions[i, 2];

            Vector3 value0 = cellPoints[Util.Map3DTo1D (v0, chunk.size)];
            Vector3 value1 = cellPoints[Util.Map3DTo1D (v1, chunk.size)];
            Vector3 value2 = cellPoints[Util.Map3DTo1D (v2, chunk.size)];
            Vector3 value3 = cellPoints[Util.Map3DTo1D (v3, chunk.size)];

            if (value0 != Vector3.zero &&
                value1 != Vector3.zero &&
                value2 != Vector3.zero &&
                value3 != Vector3.zero) {
                if (chunk.voxels.GetVoxel (v2).density > 0) {
                    vertices.Add (value0);
                    vertices.Add (value1);
                    vertices.Add (value2);
                    vertices.Add (value3);
                } else {
                    vertices.Add (value0);
                    vertices.Add (value3);
                    vertices.Add (value2);
                    vertices.Add (value1);
                }
            }
        }
    }

    private Vector3 GetNormal (Vector3 v, Func<Vector3, float> densityFunction) {
        float offset = 0.001f;
        float dx = densityFunction (new Vector3 (v.x + offset, v.y, v.z)) - densityFunction (new Vector3 (v.x - offset, v.y, v.z));
        float dy = densityFunction (new Vector3 (v.x, v.y + offset, v.z)) - densityFunction (new Vector3 (v.x, v.y - offset, v.z));
        float dz = densityFunction (new Vector3 (v.x, v.y, v.z + offset)) - densityFunction (new Vector3 (v.x, v.y, v.z - offset));

        var gradient = new Vector3 (-dx, -dy, -dz).normalized;
        return gradient;
    }
}