using System.Collections.Generic;
using UnityEngine;

public class MarchingCubesMeshGenerator : ChunkMeshGenerator
{



    public override MeshData GenerateMesh(Chunk chunk)
    {
        // float isoLevel = .4f;
        // var vertices = new List<Vector3>();
        // var triangles = new List<int>();

        // for (int x = 0; x < chunk.Size - 1; x++)
        //     for (int y = 0; y < chunk.Size - 1; y++)
        //         for (int z = 0; z < chunk.Size - 1; z++)
        //         {
        //             float[] cubeDensity = new float[]
        //             {
        //                 chunk.Voxels[Util.Map3DTo1D(x , y , z, chunk.Size)].Density,
        //                 chunk.Voxels[Util.Map3DTo1D((x + 1) , y , z , chunk.Size)].Density,
        //                 chunk.Voxels[Util.Map3DTo1D((x + 1) , y , (z + 1) , chunk.Size)].Density,
        //                 chunk.Voxels[Util.Map3DTo1D(x , y , (z + 1) , chunk.Size)].Density,
        //                 chunk.Voxels[Util.Map3DTo1D(x , (y + 1) , z , chunk.Size)].Density,
        //                 chunk.Voxels[Util.Map3DTo1D((x + 1) , (y  + 1) , z , chunk.Size)].Density,
        //                 chunk.Voxels[Util.Map3DTo1D((x + 1) , (y + 1) , (z + 1) , chunk.Size)].Density,
        //                 chunk.Voxels[Util.Map3DTo1D(x , (y + 1) , (z  + 1) , chunk.Size)].Density
        //             };

        //             Vector3[] cubeVectors = new Vector3[]
        //             {
        //                 new Vector3(x , y , z ),
        //                 new Vector3((x + 1) , y , z ),
        //                 new Vector3((x + 1) , y , (z + 1)),
        //                 new Vector3(x , y , (z + 1) ),
        //                 new Vector3(x , (y + 1) , z ),
        //                 new Vector3((x + 1) , (y  + 1) , z ),
        //                 new Vector3((x + 1) , (y + 1) , (z + 1) ),
        //                 new Vector3(x , (y + 1) , (z  + 1) ),
        //             };


        //             int cubeindex = 0;
        //             if (cubeDensity[0] < isoLevel) cubeindex |= 1;
        //             if (cubeDensity[1] < isoLevel) cubeindex |= 2;
        //             if (cubeDensity[2] < isoLevel) cubeindex |= 4;
        //             if (cubeDensity[3] < isoLevel) cubeindex |= 8;
        //             if (cubeDensity[4] < isoLevel) cubeindex |= 16;
        //             if (cubeDensity[5] < isoLevel) cubeindex |= 32;
        //             if (cubeDensity[6] < isoLevel) cubeindex |= 64;
        //             if (cubeDensity[7] < isoLevel) cubeindex |= 128;

        //             int[] triangulation = Lookup.triTabl[cubeindex];

        //             for (int i = 0; triangulation[i] != -1; i += 3)
        //             {

        //                 var points = new Vector3[3];

        //                 for (int j = 0; j < 3; j++)
        //                 {
        //                     var a0 = Lookup.cornerIndexAFromEdge[triangulation[i + j]];
        //                     var b0 = Lookup.cornerIndexBFromEdge[triangulation[i + j]];
        //                     // points[j] = Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0]));
        //                     vertices.Add(Vector3.Lerp(cubeVectors[a0], cubeVectors[b0], (isoLevel - cubeDensity[a0]) / (cubeDensity[b0] - cubeDensity[a0])));
        //                 }
        //                 // triangles.Add(new Triangle(points));
        //             }


        //         }
        var meshData = new MeshData();
        return meshData;
    }
}