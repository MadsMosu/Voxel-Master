using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelMaster.Chunk;

public static class VoxelSplitter {

    public static BoundsInt voxelSpaceBound;
    static FastNoise noise = new FastNoise();
    public static void Split(VoxelObject voxelObject) {
        noise.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
        noise.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Sub);
        float noiseScale = (voxelObject.chunkSize.magnitude) / 600f;

        voxelObject.chunk.voxels.Traverse((x, y, z, v) => {

            var coord = new Vector3Int(x, y, z);


            var n = 1f - noise.GetCellular(x / noiseScale, y / noiseScale, z / noiseScale);

            var dist = 1.5f - n;
            n *= dist;
            n = Mathf.Clamp01(n);
            n = 1f - n;

            voxelObject.chunk.voxels.SetVoxel(coord, new Voxel(n * v.density));


        });

        voxelObject.UpdateMesh();


        VoxelSplitter.SeparateIslands(voxelObject.chunk, voxelObject.transform, voxelObject.material);
    }

    public static void SeparateIslands(VoxelChunk chunk, Transform transform, Material material) {
        int voxelCount = chunk.size.x * chunk.size.y * chunk.size.z;
        DisjointSet linked = new DisjointSet(voxelCount);
        int[] labels = new int[voxelCount];
        int currentLabel = 1;

        chunk.voxels.Traverse((x, y, z, voxel) => {
            if (voxel.density < .5f) return;

            List<int> neighborLabels = new List<int>();
            for (int nx = -1; nx <= 1; nx++)
                for (int ny = -1; ny <= 1; ny++)
                    for (int nz = -1; nz <= 1; nz++) {
                        if (nx == 0 && ny == 0 && nz == 0) continue;
                        if (nx != 0 && ny != 0 || ny != 0 && nz != 0 || nz != 0 && nx != 0) continue;
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
        }
        int highestLabel = labels.Max();
        //EXTRACT VOXELS BASED ON LABELS AND ASSIGN TO NEW CHUNKS
        if (highestLabel > 1) {
            for (int i = 1; i <= highestLabel; i++) {

                if (labels.Count(x => x == i) < 4) continue;

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

                Voxel[] regionVoxels = chunk.voxels.ExtractRegion(voxelSpaceBound, labels, i);
                GameObject go = new GameObject();
                go.layer = 8;
                go.transform.position = transform.TransformPoint(voxelSpaceBound.min);
                VoxelObject voxelObject = go.AddComponent<VoxelObject>();
                voxelObject.chunkSize = voxelSpaceBound.size + Vector3Int.one;
                voxelObject.chunk = new VoxelChunk(Vector3Int.zero, voxelSpaceBound.size + Vector3Int.one, chunk.voxelScale, new SimpleDataStructure());
                voxelObject.chunk.voxels.SetVoxels(regionVoxels);
                voxelObject.material = material;
                voxelObject.original = false;

                var rb = go.AddComponent<Rigidbody>();
                rb.velocity = voxelObject.GetComponent<Rigidbody>().velocity * 99;
            };
            GameObject.Destroy(transform.gameObject);
        }
    }
}