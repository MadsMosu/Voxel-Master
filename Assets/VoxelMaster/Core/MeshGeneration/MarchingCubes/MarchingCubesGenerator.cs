using System;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubes : VoxelMeshGenerator {
    public override MeshData GenerateMesh (VoxelChunk chunk, Func<Vector3, float> densityFunction) {
        int numCells = chunk.size.x * chunk.size.y * chunk.size.z;
        var vertices = new List<Vector3> (5 * numCells * 3);
        var triangleIndicies = new List<int> (5 * numCells * 3);
        int triangleIndex = 0;

        chunk.voxels.Traverse (delegate (int x, int y, int z, Voxel v) {
            var cellPos = new Vector3Int (x, y, z);
            if (cellPos.x + 1 >= chunk.size.x || cellPos.y + 1 >= chunk.size.y || cellPos.z + 1 >= chunk.size.z) return;

            float[] cubeDensity = new float[8] {
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[0]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[1]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[2]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[3]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[4]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[5]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[6]).density,
                chunk.voxels.GetVoxel (cellPos + Lookup.cubeVertOffsets[7]).density,
            };

            int cubeindex = 0;
            if (cubeDensity[0] < chunk.isoLevel) cubeindex |= 1;
            if (cubeDensity[1] < chunk.isoLevel) cubeindex |= 2;
            if (cubeDensity[2] < chunk.isoLevel) cubeindex |= 4;
            if (cubeDensity[3] < chunk.isoLevel) cubeindex |= 8;
            if (cubeDensity[4] < chunk.isoLevel) cubeindex |= 16;
            if (cubeDensity[5] < chunk.isoLevel) cubeindex |= 32;
            if (cubeDensity[6] < chunk.isoLevel) cubeindex |= 64;
            if (cubeDensity[7] < chunk.isoLevel) cubeindex |= 128;

            int[] triangulation = Lookup.triTable[cubeindex];
            for (int i = 0; triangulation[i] != -1; i += 3) {
                for (int j = 0; j < 3; j++) {
                    var a = Lookup.cornerIndexAFromEdge[triangulation[i + j]];
                    var b = Lookup.cornerIndexBFromEdge[triangulation[i + j]];

                    Vector3Int aPos = cellPos + Lookup.cubeVertOffsets[a];
                    Vector3Int bPos = cellPos + Lookup.cubeVertOffsets[b];
                    float lerp = (chunk.isoLevel - cubeDensity[a]) / (cubeDensity[b] - cubeDensity[a]);
                    var vertex = Vector3.Lerp (aPos, bPos, lerp);

                    vertices.Add (vertex);
                    triangleIndicies.Add (triangleIndex++);
                }
            }
        });
        return new MeshData (vertices.ToArray (), triangleIndicies.ToArray ());
    }
}