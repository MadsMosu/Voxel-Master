using System;
using System.Collections.Generic;
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

    private List<Vector3> normals;
    private List<Vector3> vertices;
    private List<int> triangleIndicies;
    private Vector3[] cellPoints;
    private Vector3[] verticeNormals;
    private readonly static float QEF_ERROR = 1e-6f;
    private readonly static int QEF_SWEEPS = 16;
    // private Vector3[] normalField;

    public override MeshData GenerateMesh (VoxelChunk chunk, Func<Vector3, float> densityFunction) {
        vertices = new List<Vector3> ();
        triangleIndicies = new List<int> ();
        normals = new List<Vector3> ();
        cellPoints = new Vector3[chunk.size.x * chunk.size.y * chunk.size.z];
        verticeNormals = new Vector3[chunk.size.x * chunk.size.y * chunk.size.z];
        // normalField = new Vector3[chunk.size.x * chunk.size.y * chunk.size.z];

        // chunk.voxels.Traverse (delegate (int x, int y, int z, Voxel v) {
        //     Vector3Int cellPos = new Vector3Int (x, y, z);
        //     if (cellPos.x >= chunk.size.x - 1 || cellPos.y >= chunk.size.y - 1 || cellPos.z >= chunk.size.z - 1) return;

        //     float dx = v.density - chunk.voxels.GetVoxel (cellPos + new Vector3Int (1, 0, 0)).density;
        //     float dy = v.density - chunk.voxels.GetVoxel (cellPos + new Vector3Int (0, 1, 0)).density;
        //     float dz = v.density - chunk.voxels.GetVoxel (cellPos + new Vector3Int (0, 0, 1)).density;

        //     normalField[Util.Map3DTo1D (cellPos, chunk.size)] = Vector3.Normalize (new Vector3 (dx, dy, dz));
        // });

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
        if (cellPos.x + 2 >= chunk.size.x || cellPos.y + 2 >= chunk.size.y || cellPos.z + 2 >= chunk.size.z) return;

        int corners = 0;
        float[] cellDensities = new float[8];
        for (int i = 0; i < 8; i++) {
            float density = chunk.voxels.GetVoxel (cellPos + vertOffsets[i]).density;
            cellDensities[i] = density;
            if (density < chunk.isoLevel) {
                corners |= 1 << i;
            }
        }
        // if all of the corners are either outside or inside of surface boundary
        if (corners == 0 || corners == 255) return;

        QefSolver qef = new QefSolver ();
        Vector3 averageNormal = new Vector3 ();
        int activeEdges = 0;
        int MAX_CROSSINGS = 12;
        for (int i = 0; i < edges.Length && activeEdges <= MAX_CROSSINGS; i++) {
            Vector2Int edge = edges[i];
            int m1 = (corners >> edge.x) & 0b1;
            int m2 = (corners >> edge.y) & 0b1;

            // check for zero crossing point. the point of where the sign function changes (eg. positive to negative or negative to positive)
            // only continue if there is no zero crossing (position of the surface). no need to make vertex inside the cube when there is no surface border
            if (m1 == m2) continue;

            Vector3 aOffset = vertOffsets[edge.x];
            Vector3 bOffset = vertOffsets[edge.y];

            Vector3Int aPos = cellPos + vertOffsets[edge.x];
            Vector3Int bPos = cellPos + vertOffsets[edge.y];

            float aDensity = cellDensities[edge.x];
            float bDensity = cellDensities[edge.y];

            float lerp = (chunk.isoLevel - aDensity) / (bDensity - aDensity);
            var intersectionPoint = Vector3.Lerp (aOffset + cellPos, bOffset + cellPos, 0f);

            // var normal = new Vector3 (0, 0, 0);
            var normal = GetNormal (intersectionPoint, densityFunction);
            averageNormal += normal;

            qef.add (intersectionPoint.x, intersectionPoint.y, intersectionPoint.z, normal.x, normal.y, normal.z);
            activeEdges++;
        }
        //the vertex that is clostest to the surface
        Vector3 qefPosition = Vector3.zero;
        qef.solve (qefPosition, QEF_ERROR, QEF_SWEEPS, QEF_ERROR);
        var vertex = qef.getMassPoint ();

        cellPoints[Util.Map3DTo1D (cellPos, chunk.size)] = vertex;
        verticeNormals[Util.Map3DTo1D (cellPos, chunk.size)] = Vector3.Normalize (averageNormal / (float) activeEdges);
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

                Vector3 normal0 = verticeNormals[Util.Map3DTo1D (v0, chunk.size)];
                Vector3 normal1 = verticeNormals[Util.Map3DTo1D (v1, chunk.size)];
                Vector3 normal2 = verticeNormals[Util.Map3DTo1D (v2, chunk.size)];
                Vector3 normal3 = verticeNormals[Util.Map3DTo1D (v3, chunk.size)];

                if (chunk.voxels.GetVoxel (v2).density > chunk.isoLevel) {
                    vertices.Add (value0);
                    vertices.Add (value1);
                    vertices.Add (value2);
                    vertices.Add (value3);

                    // normals.Add (normal0);
                    // normals.Add (normal1);
                    // normals.Add (normal2);
                    // normals.Add (normal3);
                } else {
                    vertices.Add (value0);
                    vertices.Add (value3);
                    vertices.Add (value2);
                    vertices.Add (value1);

                    // normals.Add (normal0);
                    // normals.Add (normal3);
                    // normals.Add (normal2);
                    // normals.Add (normal1);
                }
            }
        }
    }

    private Vector3 GetNormal (Vector3 v, Func<Vector3, float> densityFunction) {
        float offset = 0.0001f;
        float dx = densityFunction (new Vector3 (v.x + offset, v.y, v.z)) - densityFunction (new Vector3 (v.x - offset, v.y, v.z));
        float dy = densityFunction (new Vector3 (v.x, v.y + offset, v.z)) - densityFunction (new Vector3 (v.x, v.y - offset, v.z));
        float dz = densityFunction (new Vector3 (v.x, v.y, v.z + offset)) - densityFunction (new Vector3 (v.x, v.y, v.z - offset));

        var gradient = new Vector3 (-dx, -dy, -dz).normalized;
        return gradient;
    }
}