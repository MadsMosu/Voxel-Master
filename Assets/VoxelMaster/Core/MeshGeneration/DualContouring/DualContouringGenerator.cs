using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DualContouring : VoxelMeshGenerator
{

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

    private Vector3 Lerp(Vector3 v0, Vector3 v1, float t)
    {
        return (1 - t) * v0 + t * v1;
    }

    private float InverseLerp(float a, float b, float t)
    {
        if (a != b)
        {
            return (t - a / (b - a));
        }
        return 0;
    }

    public override MeshData GenerateMesh(VoxelChunk chunk, Func<Vector3, float> densityFunction)
    {
        var vertices = new List<Vector3>();
        var triangleIndicies = new List<int>();
        var normals = new List<Vector3>();
        var cellPoints = new Vector3[chunk.size.x * chunk.size.y * chunk.size.z];
        var cellPointIndicies = new int[chunk.size.x * chunk.size.y * chunk.size.z];

        // for each cell that exhibits a sign change, a vertex is generated and positioned at the edge with the lowest error (using QEF)
        for (int x = 0; x < chunk.size.x - 1; x++)
            for (int y = 0; y < chunk.size.y - 1; y++)
                for (int z = 0; z < chunk.size.z - 1; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, z);
                    int corners = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        float density = chunk.voxels.GetVoxel(cellPos + vertOffsets[i]).density;
                        if (density < chunk.isoLevel)
                        {
                            corners |= 1 << i;
                        }
                    }
                    // if all of the corners are either outside or inside of surface boundary
                    if (corners == 0 || corners == 255) continue;

                    QEF3D qef = new QEF3D();
                    Vector3 averageNormal = new Vector3();
                    for (int i = 0; i < edges.Length; i++)
                    {
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

                        float aDensity = chunk.voxels.GetVoxel(aPos).density;
                        float bDensity = chunk.voxels.GetVoxel(bPos).density;

                        var intersectionPoint = Lerp(aPos, bPos, InverseLerp(aDensity, bDensity, chunk.isoLevel));
                        // var intersectionPoint = (aOffset + (-aDensity) * (bOffset - aOffset) / (bDensity - aDensity));

                        var normal = GetNormal(intersectionPoint, densityFunction);
                        averageNormal += normal;

                        qef.Add(intersectionPoint, normal);
                    }
                    //the vertex that is clostest to the surface
                    var vertex = qef.Solve();
                    var averagedNormal = Vector3.Normalize(averageNormal / (float)qef.Intersections.Count);
                    Vector3 c_v = averagedNormal * 0.5f + Vector3.one * 0.5f;
                    c_v.Normalize();
                    normals.Add(averagedNormal);

                    // cellPointIndicies[Util.Map3DTo1D(cellPos, chunk.size)] = vertices.Count;
                    cellPoints[Util.Map3DTo1D(cellPos, chunk.size)] = vertex;
                    // vertices.Add(vertex);
                }

        // for each of the edges that went through a sign change, triangles are generated connecting the vertices of the four cubes adjacent to the edge
        for (int x = 0; x < chunk.size.x - 1; x++)
            for (int y = 0; y < chunk.size.y - 1; y++)
                for (int z = 0; z < chunk.size.z - 1; z++)
                {
                    var cellPos = new Vector3Int(x, y, z);
                    var v0 = cellPos;
                    var v1 = v0 + new Vector3Int(1, 0, 0);
                    var v2 = v0 + new Vector3Int(1, 0, 1);
                    var v3 = v0 + new Vector3Int(0, 0, 1);

                    Vector3 value0 = cellPoints[Util.Map3DTo1D(v0, chunk.size)];
                    Vector3 value1 = cellPoints[Util.Map3DTo1D(v1, chunk.size)];
                    Vector3 value2 = cellPoints[Util.Map3DTo1D(v2, chunk.size)];
                    Vector3 value3 = cellPoints[Util.Map3DTo1D(v3, chunk.size)];

                    if (value0 != Vector3.zero &&
                        value1 != Vector3.zero &&
                        value2 != Vector3.zero &&
                        value3 != Vector3.zero)
                    {
                        if (chunk.voxels.GetVoxel(v2).density > 0)
                        {
                            vertices.Add(value0);
                            vertices.Add(value1);
                            vertices.Add(value2);
                            vertices.Add(value3);
                        }
                        else
                        {
                            vertices.Add(value0);
                            vertices.Add(value3);
                            vertices.Add(value2);
                            vertices.Add(value1);
                        }
                    }

                    v1 = v0 + new Vector3Int(0, 1, 0);
                    v2 = v0 + new Vector3Int(1, 1, 0);
                    v3 = v0 + new Vector3Int(1, 0, 0);

                    value1 = cellPoints[Util.Map3DTo1D(v1, chunk.size)];
                    value2 = cellPoints[Util.Map3DTo1D(v2, chunk.size)];
                    value3 = cellPoints[Util.Map3DTo1D(v3, chunk.size)];

                    if (value0 != Vector3.zero &&
                        value1 != Vector3.zero &&
                        value2 != Vector3.zero &&
                        value3 != Vector3.zero)
                    {
                        if (chunk.voxels.GetVoxel(v2).density > 0)
                        {
                            vertices.Add(value0);
                            vertices.Add(value1);
                            vertices.Add(value2);
                            vertices.Add(value3);
                        }
                        else
                        {
                            vertices.Add(value0);
                            vertices.Add(value3);
                            vertices.Add(value2);
                            vertices.Add(value1);
                        }
                    }

                    v1 = v0 + new Vector3Int(0, 0, 1);
                    v2 = v0 + new Vector3Int(0, 1, 1);
                    v3 = v0 + new Vector3Int(0, 1, 0);

                    value1 = cellPoints[Util.Map3DTo1D(v1, chunk.size)];
                    value2 = cellPoints[Util.Map3DTo1D(v2, chunk.size)];
                    value3 = cellPoints[Util.Map3DTo1D(v3, chunk.size)];

                    if (value0 != Vector3.zero &&
                        value1 != Vector3.zero &&
                        value2 != Vector3.zero &&
                        value3 != Vector3.zero)
                    {
                        if (chunk.voxels.GetVoxel(v2).density > 0)
                        {
                            vertices.Add(value0);
                            vertices.Add(value1);
                            vertices.Add(value2);
                            vertices.Add(value3);
                        }
                        else
                        {
                            vertices.Add(value0);
                            vertices.Add(value3);
                            vertices.Add(value2);
                            vertices.Add(value1);
                        }
                    }
                }

        for (int i = 0; i < vertices.Count; i += 4)
        {
            triangleIndicies.Add(i);
            triangleIndicies.Add(i + 1);
            triangleIndicies.Add(i + 2);

            triangleIndicies.Add(i + 2);
            triangleIndicies.Add(i + 3);
            triangleIndicies.Add(i);
        }
        return new MeshData(vertices.ToArray(), triangleIndicies.ToArray());
    }

    private Vector3 GetNormal(Vector3 v, Func<Vector3, float> densityFunction)
    {
        float offset = 0.001f;
        float dx = densityFunction(new Vector3(v.x + offset, v.y, v.z)) - densityFunction(new Vector3(v.x - offset, v.y, v.z));
        float dy = densityFunction(new Vector3(v.x, v.y + offset, v.z)) - densityFunction(new Vector3(v.x, v.y - offset, v.z));
        float dz = densityFunction(new Vector3(v.x, v.y, v.z + offset)) - densityFunction(new Vector3(v.x, v.y, v.z - offset));

        var gradient = new Vector3(-dx, -dy, -dz).normalized;
        return gradient;
    }
}