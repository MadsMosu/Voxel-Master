using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class MarchingCubes
{


    public static void GenerateMesh(Chunk chunk, out List<Triangle> triangles, float isoLevel = 0.5f)
    {
        triangles = new List<Triangle>();

        for (int x = 0; x < (chunk.Voxels.GetLength(0) - 1); x++)
            for (int y = 0; y < (chunk.Voxels.GetLength(1) - 1); y++)
                for (int z = 0; z < (chunk.Voxels.GetLength(2) - 1); z++)
                {


                    float[] cubeDensity = new float[]
                    {
                        chunk.Voxels[x , y , z ].Density,
                        chunk.Voxels[(x + 1) , y , z ].Density,
                        chunk.Voxels[(x + 1) , y , (z + 1) ].Density,
                        chunk.Voxels[x , y , (z + 1) ].Density,
                        chunk.Voxels[x , (y + 1) , z ].Density,
                        chunk.Voxels[(x + 1) , (y  + 1) , z ].Density,
                        chunk.Voxels[(x + 1) , (y + 1) , (z + 1) ].Density,
                        chunk.Voxels[x , (y + 1) , (z  + 1) ].Density
                    };

                    Vector3[] cubeVectors = new Vector3[]
                    {
                        new Vector3(x , y , z ),
                        new Vector3((x + 1) , y , z ),
                        new Vector3((x + 1) , y , (z + 1) ),
                        new Vector3(x , y , (z + 1) ),
                        new Vector3(x , (y + 1) , z ),
                        new Vector3((x + 1) , (y  + 1) , z ),
                        new Vector3((x + 1) , (y + 1) , (z + 1) ),
                        new Vector3(x , (y + 1) , (z  + 1) ),
                    };


                    int cubeindex = 0;
                    if (cubeDensity[0] < isoLevel) cubeindex |= 1;
                    if (cubeDensity[1] < isoLevel) cubeindex |= 2;
                    if (cubeDensity[2] < isoLevel) cubeindex |= 4;
                    if (cubeDensity[3] < isoLevel) cubeindex |= 8;
                    if (cubeDensity[4] < isoLevel) cubeindex |= 16;
                    if (cubeDensity[5] < isoLevel) cubeindex |= 32;
                    if (cubeDensity[6] < isoLevel) cubeindex |= 64;
                    if (cubeDensity[7] < isoLevel) cubeindex |= 128;

                    int[] triangulation = Lookup.triTabl[cubeindex];

                    for (int i = 0; triangulation[i] != -1; i += 3)
                    {

                        var points = new Vector3[3];

                        for (int j = 0; j < 3; j++)
                        {
                            var a0 = Lookup.cornerIndexAFromEdge[triangulation[i + j]];
                            var b0 = Lookup.cornerIndexBFromEdge[triangulation[i + j]];
                            points[j] = Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0]));
                        }

                        triangles.Add(new Triangle(points));
                    }

                }


    }


}
