using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DualContouring2 : VoxelMeshGenerator
{

    private readonly static Vector2Int[] edges = new Vector2Int[12]
    {
        new Vector2Int(0,4), new Vector2Int(1,5), new Vector2Int(2,6), new Vector2Int(3,7), //x-axis
        new Vector2Int(0,1), new Vector2Int(2,3), new Vector2Int(4,5), new Vector2Int(6,7), //y-axis
        new Vector2Int(0,2), new Vector2Int(1,3), new Vector2Int(4,6), new Vector2Int(5,7) //z-axis
        // new Vector2Int(0,4), new Vector2Int(1,5), new Vector2Int(2,6), new Vector2Int(3,7), //x-axis
        // new Vector2Int(0,1), new Vector2Int(2,3), new Vector2Int(4,5), new Vector2Int(6,7), //y-axis
        // new Vector2Int(0,2), new Vector2Int(1,3), new Vector2Int(4,6), new Vector2Int(5,7) //z-axis
    };

    private readonly static Vector3Int[] vertOffsets = new Vector3Int[8]
    {
        // new Vector3Int(0, 0, 0),
        // new Vector3Int(0, 0, 1),
        // new Vector3Int(0, 1, 0),
        // new Vector3Int(0, 1, 1),
        // new Vector3Int(1, 0, 0),
        // new Vector3Int(1, 0, 1),
        // new Vector3Int(1, 1, 0),
        // new Vector3Int(1, 1, 1)
        new Vector3Int(0, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 1),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, 1),
    };

    private readonly static Vector3Int[] directions = new Vector3Int[3] {
        new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, 0, 1)
    };

    private struct CellEdge
    {
        public Vector3 intersectionPoint;
        public Vector3[] cellPoints;
        public bool flip;
        public bool processed;
        public float aDensity;
        public float bDensity;
        public bool hasIntersection;

        public CellEdge(Vector3Int a, Vector3Int b, int m1, float aDensity, float bDensity, float isoLevel)
        {
            hasIntersection = false;
            this.processed = true;
            this.cellPoints = new Vector3[4];
            if (aDensity < isoLevel && bDensity > isoLevel)
            {
                hasIntersection = true;
                this.flip = false;
                this.aDensity = aDensity;
                this.bDensity = bDensity;
                this.intersectionPoint = Vector3.Lerp(a, b, (isoLevel - aDensity) / (bDensity - aDensity));
            }
            else
            {
                hasIntersection = true;
                this.flip = true;
                this.bDensity = aDensity;
                this.aDensity = bDensity;
                this.intersectionPoint = Vector3.Lerp(a, b, (isoLevel - aDensity) / (bDensity - aDensity));
            }
        }
    }

    private List<Vector3> vertices;
    private List<int> triangleIndicies;
    private List<Vector3> normals;
    private List<CellEdge> surfaceEdges;
    private List<CellEdge> activeEdges;

    private CellEdge GetOrAdd(CellEdge ce, List<CellEdge> edgeList)
    {
        if (edgeList.Contains(ce))
        {
            return edgeList[edgeList.IndexOf(ce)];
        }
        else
        {
            edgeList.Add(ce);
            return ce;
        }
    }

    public override MeshData generateMesh(VoxelChunk chunk, Func<Vector3, float> densityFunction)
    {
        vertices = new List<Vector3>();
        triangleIndicies = new List<int>();
        normals = new List<Vector3>();
        var cellVertexIndicies = new int[chunk.size.x * chunk.size.y * chunk.size.z];
        surfaceEdges = new List<CellEdge>();
        activeEdges = new List<CellEdge>();

        // for each cell that exhibits a sign change, a vertex is generated and positioned at the edge with the lowest error (using QEF)
        for (int x = 0; x < chunk.size.x - 1; x++)
            for (int y = 0; y < chunk.size.y - 1; y++)
                for (int z = 0; z < chunk.size.z - 1; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, z);
                    CellEdge[] cubeEdges = new CellEdge[12];
                    int corners = 0;


                    for (int i = 0; i < 8; i++)
                    {
                        // cubeCorners[i] = cellPos + vertOffsets[i];
                        float density = chunk.voxels.GetVoxel(cellPos + vertOffsets[i]).density;
                        // cubeDensities[i] = density;
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

                        Vector3Int aPos = cellPos + vertOffsets[edge.x];
                        Vector3Int bPos = cellPos + vertOffsets[edge.y];

                        float aDensity = chunk.voxels.GetVoxel(aPos).density;
                        float bDensity = chunk.voxels.GetVoxel(bPos).density;

                        CellEdge ce = new CellEdge(aPos, bPos, m1, aDensity, bDensity, chunk.isoLevel);
                        // check for zero crossing point. the point of where the sign function changes (eg. positive to negative or negative to positive)
                        // only continue if there is no zero crossing (position of the surface). no need to make vertex inside the cube when there is no surface border
                        if (m1 == m2) continue;

                        // Vector3 aOffset = vertOffsets[edge.x];
                        // Vector3 bOffset = vertOffsets[edge.y];

                        // var intersectionPoint = (aOffset + (-aDensity) * (bOffset - aOffset) / (bDensity - aDensity));
                        cubeEdges[i] = ce;
                        var normal = GetNormal(ce.intersectionPoint + cellPos, densityFunction);
                        averageNormal += normal;

                        qef.Add(ce.intersectionPoint, normal);
                    }
                    if (cubeEdges.Length == 0 || cubeEdges.Length == 8) continue;

                    //the vertex that is clostest to the surface
                    var vertex = qef.Solve() + cellPos;
                    int k = 0;
                    foreach (CellEdge ce in cubeEdges)
                    {
                        if (ce.hasIntersection)
                        {
                            CellEdge c = GetOrAdd(ce, surfaceEdges);
                            c.cellPoints[k] = vertex;
                        }
                        k++;
                        if (k > 3) k = 0;
                    }

                    var averagedNormal = Vector3.Normalize(averageNormal / (float)qef.Intersections.Count);
                    Vector3 c_v = averagedNormal * 0.5f + Vector3.one * 0.5f;
                    c_v.Normalize();
                    normals.Add(averagedNormal);

                    // cellVertexIndicies[Util.Map3DTo1D(cellPos, chunk.size)] = vertices.Count;
                    // vertices.Add(vertex);
                }

        foreach (CellEdge ce in surfaceEdges)
        {
            if (ce.cellPoints.Count(v => v != Vector3.zero) == 4)
            {
                GetOrAdd(ce, activeEdges);
                surfaceEdges.Remove(ce);
            }
        }

        foreach (CellEdge ce in activeEdges)
        {
            if (ce.flip)
            {
                vertices.Add(ce.cellPoints[0]);
                vertices.Add(ce.cellPoints[2]);
                vertices.Add(ce.cellPoints[1]);

                vertices.Add(ce.cellPoints[2]);
                vertices.Add(ce.cellPoints[3]);
                vertices.Add(ce.cellPoints[1]);
            }
            else
            {
                vertices.Add(ce.cellPoints[0]);
                vertices.Add(ce.cellPoints[1]);
                vertices.Add(ce.cellPoints[2]);

                vertices.Add(ce.cellPoints[2]);
                vertices.Add(ce.cellPoints[1]);
                vertices.Add(ce.cellPoints[3]);
            }
        }

        for (int i = 0; i < vertices.Count; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                triangleIndicies[i + j] = i + j;

            }
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