using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelMaster.Chunk;

public static class VoxelSplitter {

    public static BoundsInt voxelSpaceBound;
    public static void Split(VoxelObject voxelObject) {
        FastNoise noise = new FastNoise();
        noise.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
        noise.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Sub);
        float noiseScale = .1f;

        float min = 0;
        float max = 0;

        voxelObject.chunk.voxels.Traverse((x, y, z, v) => {
            var voronoi = noise.GetCellular(x / noiseScale, y / noiseScale, z / noiseScale) - .4f;
            voronoi = Mathf.Pow(voronoi, 10);


            if (voronoi > max) max = voronoi;
            if (voronoi < min) min = voronoi;

            var coord = new Vector3Int(x, y, z);
            voxelObject.chunk.voxels.SetVoxel(coord, new Voxel { density = v.density - voronoi*32*32*32, materialIndex = 0 });

            //if (x == Mathf.FloorToInt(voxelObject.chunkSize.x / 2) || y == Mathf.FloorToInt(voxelObject.chunkSize.y / 2) || z == Mathf.FloorToInt(voxelObject.chunkSize.z / 2)) {
            //    var coord = new Vector3Int(x, y, z);
            //    voxelObject.chunk.voxels.SetVoxel(coord, new Voxel { density = -1, materialIndex = 0 });
            //}
        });

        voxelObject.UpdateMesh();

        Debug.Log($"min: {min}\tmax: {max}");
        
        VoxelSplitter.SeparateIslands(voxelObject.chunk, voxelObject.transform, voxelObject.material);
    }

    public static void SeparateIslands(VoxelChunk chunk, Transform transform, Material material) {
        DisjointSet linked = new DisjointSet(chunk.voxels.ToArray().Length);
        int[] labels = new int[chunk.voxels.ToArray().Length];
        int currentLabel = 1;

        chunk.voxels.Traverse((x, y, z, voxel) => {
            if (voxel.density < 0f) return;

            List<int> neighborLabels = new List<int>();
            for (int nx = -1; nx <= 1; nx++)
                for (int ny = -1; ny <= 1; ny++)
                    for (int nz = -1; nz <= 1; nz++) {
                        if (nx == 0 && ny == 0 && nz == 0) continue;
                        if (
                            (x + nx < 0 || x + nx >= chunk.size.x) ||
                            (y + ny < 0 || y + ny >= chunk.size.y) ||
                            (z + nz < 0 || z + nz >= chunk.size.z)


                        ) continue;

                        var neighborLabel = labels[Util.Map3DTo1D(new Vector3Int(x + nx, y + ny, z + nz), chunk.size)];
                        if (neighborLabel > 0) neighborLabels.Add(neighborLabel);

                    }
            if (neighborLabels.Count == 0) {
                linked.MakeSet(currentLabel);
                labels[Util.Map3DTo1D(new Vector3Int(x, y, z), chunk.size)] = currentLabel;
                currentLabel++;
            }
            else {
                int smallestLabel = neighborLabels.OrderBy(label => label).First();
                labels[Util.Map3DTo1D(new Vector3Int(x, y, z), chunk.size)] = smallestLabel;
                for (int n = 0; n < neighborLabels.Count; n++) {
                    linked.Union(neighborLabels[n], neighborLabels.ToArray());
                }
            }
        });

        for (int i = 0; i < labels.Length; i++) {
            labels[i] = linked.Find(labels[i]);
            var coord = Util.Map1DTo3D(i, chunk.size);
            var voxel = chunk.voxels.GetVoxel(coord);
            chunk.voxels.SetVoxel(coord, new Voxel { density = voxel.density, materialIndex = (byte)labels[i] });
        }

        int highestLabel = labels.Max();
        //EXTRACT VOXELS BASED ON LABELS AND ASSIGN TO NEW CHUNKS
        if (highestLabel > 1) {
            for (int i = 1; i <= highestLabel; i++) {

                Vector3 startingVoxelPos = Util.Map1DTo3D(Array.IndexOf(labels, i), chunk.size);
                Bounds bound = new Bounds(new Vector3(startingVoxelPos.x * chunk.voxelScale, startingVoxelPos.y * chunk.voxelScale, startingVoxelPos.z * chunk.voxelScale), Vector3.zero);

                for (int x = 0; x < chunk.size.x; x++)
                    for (int y = 0; y < chunk.size.y; y++)
                        for (int z = 0; z < chunk.size.z; z++) {
                            Vector3Int cellPos = new Vector3Int(x, y, z);
                            if (labels[Util.Map3DTo1D(cellPos, chunk.size)] != i) continue;
                            bound.Encapsulate(new Vector3(cellPos.x * chunk.voxelScale, cellPos.y * chunk.voxelScale, cellPos.z * chunk.voxelScale));
                        }

                voxelSpaceBound = new BoundsInt(
                    Mathf.FloorToInt(bound.min.x / chunk.voxelScale) - 1, Mathf.FloorToInt(bound.min.y / chunk.voxelScale) - 1, Mathf.FloorToInt(bound.min.z / chunk.voxelScale) - 1,
                    Mathf.CeilToInt(bound.size.x / chunk.voxelScale) + 2, Mathf.CeilToInt(bound.size.y / chunk.voxelScale) + 2, Mathf.CeilToInt(bound.size.z / chunk.voxelScale) + 2
                );

                Voxel[] regionVoxels = chunk.voxels.ExtractRegion(voxelSpaceBound);
                GameObject go = new GameObject();
                go.layer = 8;
                go.transform.position = transform.position + voxelSpaceBound.min;
                VoxelObject voxelObject = go.AddComponent<VoxelObject>();
                voxelObject.chunkSize = voxelSpaceBound.size + Vector3Int.one;
                voxelObject.chunk = new VoxelChunk(Vector3Int.zero, voxelSpaceBound.size + Vector3Int.one, chunk.voxelScale, new SimpleDataStructure());
                voxelObject.chunk.voxels.SetVoxels(regionVoxels);
                voxelObject.material = material;
                voxelObject.original = false;

                go.AddComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
            };
            GameObject.Destroy(transform.gameObject);
        }
    }
}