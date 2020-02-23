// using System;
// using System.Collections.Generic;
// using UnityEngine;

// public class DC3D : VoxelMeshGenerator
// {

//     float[,,] map;
//     int[,,] vertex_indexes;
//     UnityEngine.Random rnd = new UnityEngine.Random();

//     int[,] edges;

//     int[,] dirs;

//     int VertexCount;
//     int IndexCount;
//     int Resolution;

//     List<Vector3> vertices;
//     List<int> triangles;

//     public void Contour()
//     {
//         vertices = new List<Vector3>();
//         triangles = new List<int>();
//         map = new float[Resolution, Resolution, Resolution];
//         InitData();
//         edges = new int[,]
//     {
//             {0,4},{1,5},{2,6},{3,7},	// x-axis 
// 			{0,2},{1,3},{4,6},{5,7},	// y-axis
// 			{0,1},{2,3},{4,5},{6,7}		// z-axis
// 		};
//         dirs = new int[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
//         vertex_indexes = new int[Resolution, Resolution, Resolution];
//         VertexCount = 1;
//         IndexCount = 0;

//         for (int x = 1; x < Resolution - 1; x++)
//         {
//             for (int y = 1; y < Resolution - 1; y++)
//             {
//                 for (int z = 1; z < Resolution - 1; z++)
//                 {
//                     GenerateAt(x, y, z);
//                 }
//             }
//         }

//         for (int x = 1; x < Resolution - 1; x++)
//         {
//             for (int y = 1; y < Resolution - 1; y++)
//             {
//                 for (int z = 1; z < Resolution - 1; z++)
//                 {
//                     GenerateIndexAt(x, y, z);
//                 }
//             }
//         }
//     }

//     public void GenerateAt(int x, int y, int z)
//     {
//         int corners = 0;
//         for (int i = 0; i < 8; i++)
//         {
//             if (map[x + i / 4, y + i % 4 / 2, z + i % 2] < 0)
//                 corners |= 1 << i;
//         }

//         if (corners == 0 || corners == 255)
//             return;


//         QEF3D qef = new QEF3D();
//         Vector3 average_normal = new Vector3();
//         for (int i = 0; i < 12; i++)
//         {
//             int c1 = edges[i, 0];
//             int c2 = edges[i, 1];

//             int m1 = (corners >> c1) & 1;
//             int m2 = (corners >> c2) & 1;
//             if (m1 == m2)
//                 continue;

//             float d1 = map[x + c1 / 4, y + c1 % 4 / 2, z + c1 % 2];
//             float d2 = map[x + c2 / 4, y + c2 % 4 / 2, z + c2 % 2];

//             Vector3 p1 = new Vector3((float)((c1 / 4)), (float)((c1 % 4 / 2)), (float)((c1 % 2)));
//             Vector3 p2 = new Vector3((float)((c2 / 4)), (float)((c2 % 4 / 2)), (float)((c2 % 2)));

//             Vector3 intersection = p1 + (-d1) * (p2 - p1) / (d2 - d1);

//             float h = 0.001f;
//             Vector3 v = intersection + new Vector3(x, y, z);
//             float dxp = DensityFunction(new Vector3(v.x + h, v.y, v.z));
//             float dxm = DensityFunction(new Vector3(v.x - h, v.y, v.z));
//             float dyp = DensityFunction(new Vector3(v.x, v.y + h, v.z));
//             float dym = DensityFunction(new Vector3(v.x, v.y - h, v.z));
//             float dzp = DensityFunction(new Vector3(v.x, v.y, v.z + h));
//             float dzm = DensityFunction(new Vector3(v.x, v.y, v.z - h));

//             Vector3 gradient = new Vector3(dxp - dxm, dyp - dym, dzp - dzm);


//             Vector3 normal = gradient.normalized;
//             average_normal += normal;

//             qef.Add(intersection, normal);
//         }

//         Vector3 p = qef.Solve2(0, 16, 0);
//         vertices.Add(p + new Vector3(x, y, z));

//         Vector3 n = average_normal / (float)qef.Intersections.Count;
//         vertex_indexes[x, y, z] = VertexCount;
//         VertexCount++;
//         // Debug.Log(VertexCount);
//     }

//     public void GenerateIndexAt(int x, int y, int z)
//     {
//         //int corners = 0;

//         int v1 = vertex_indexes[x, y, z];
//         if (v1 == 0)
//             return;

//         int[] indices = new int[256];

//         int index = 0;

//         for (int i = 0; i < 3; i++)
//         {
//             for (int j = 0; j < i; j++)
//             {
//                 int v2 = vertex_indexes[x + dirs[i, 0], y + dirs[i, 1], z + dirs[i, 2]];
//                 int v3 = vertex_indexes[x + dirs[j, 0], y + dirs[j, 1], z + dirs[j, 2]];
//                 int v4 = vertex_indexes[x + dirs[i, 0] + dirs[j, 0], y + dirs[i, 1] + dirs[j, 1], z + dirs[i, 2] + dirs[j, 2]];
//                 if (v2 == 0 || v3 == 0 || v4 == 0)
//                     continue;

//                 indices[index++] = v1;
//                 indices[index++] = v2;
//                 indices[index++] = v3;

//                 indices[index++] = v4;
//                 indices[index++] = v3;
//                 indices[index++] = v2;
//             }
//         }
//         if (index > 0)
//         {
//             triangles.AddRange(indices);
//         }
//     }

//     public override MeshData generateMesh(VoxelChunk chunk, Func<Vector3, float> densityFunction)
//     {
//         Resolution = chunk.size.x;
//         Contour();
//         Debug.Log(vertices.Count);
//         Debug.Log(triangles.Count);
//         return new MeshData(vertices.ToArray(), triangles.ToArray());

//     }

//     private float SignedDistanceSphere(Vector3 pos)
//     {
//         Vector3 center = new Vector3(Resolution / 2, Resolution / 2, Resolution / 2);
//         float radius = Resolution / 2.33f;
//         return Vector3.Distance(pos, center) - radius;
//     }

//     private float DensityFunction(Vector3 pos)
//     {

//         return SignedDistanceSphere(pos);
//     }

//     private void InitData()
//     {
//         vertex_indexes = new int[Resolution, Resolution, Resolution];
//         for (int x = 0; x < Resolution; x++)
//             for (int y = 0; y < Resolution; y++)
//                 for (int z = 0; z < Resolution; z++)
//                     map[x, y, z] = DensityFunction(new Vector3(x, y, z));
//     }
// }
