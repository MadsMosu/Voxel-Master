using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelMaster.Chunk;

public static class VoxelSplitter {

    public static List<Vector3Int> lastSplitSamples;

    public static BoundsInt voxelSpaceBound;
    static FastNoise noise = new FastNoise();
    public static void Split(VoxelObject voxelObject, Vector3 impactPoint) {
        Vector3Int pointOfImpact = Vector3Int.zero;
        Dictionary<Vector3Int, Voxel> fractureVoxels = new Dictionary<Vector3Int, Voxel>();

        var chunkSize = voxelObject.chunk.size;
        //List<Vector3Int> samples = PoissonSampler.GeneratePoints(voxelObject.chunk.size, 4f, impactPoint, 10).Select(sample => new Vector3Int((int)sample.x, (int)sample.y, (int)sample.z)).ToList();
        List<Vector3Int> samples = new List<Vector3Int>() {
            new Vector3Int(chunkSize.x,chunkSize.y,chunkSize.z)/2 + Vector3Int.up*5,
            new Vector3Int(chunkSize.x,chunkSize.y,chunkSize.z)/2 + Vector3Int.down*5,
            new Vector3Int(chunkSize.x,chunkSize.y,chunkSize.z)/2 + Vector3Int.right*5,
            //new Vector3Int(chunkSize.x,chunkSize.y,chunkSize.z)/2 + Vector3Int.left*5,
        };
        lastSplitSamples = samples;
        int[] labels = new int[voxelObject.chunk.size.x * voxelObject.chunk.size.y * voxelObject.chunk.size.z];

        voxelObject.chunk.voxels.Traverse((x, y, z, v) => {
            Vector3Int coords = new Vector3Int(x, y, z);

            float minDistance = Mathf.Infinity;
            int sampleIndex = 0;
            for (int i = 0; i < samples.Count; i++) {
                var dist = Vector3.Distance(coords, samples[i]);
                if (dist < minDistance) {
                    minDistance = dist;
                    sampleIndex = i;
                }
            }
            labels[Util.Map3DTo1D(coords, voxelObject.chunk.size)] = sampleIndex;
        });

        ExtractLabelSegments(labels, voxelObject.chunk, voxelObject.transform, voxelObject.material, voxelObject.prevVelocity);

        voxelObject.UpdateMesh();

        // SeparateIslands (voxelObject.chunk, voxelObject.transform, voxelObject.material, voxelObject.prevVelocity, fractureVoxels);
    }

    private static void ExtractLabelSegments(int[] labels, VoxelChunk chunk, Transform transform, Material material, Vector3 rbVel) {
        int highestLabel = labels.Max();
        // Debug.Log (highestLabel);
        //EXTRACT VOXELS BASED ON LABELS AND ASSIGN TO NEW CHUNKS
        if (highestLabel >= 1) {
            for (int i = 0; i <= highestLabel; i++) {

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
                go.transform.position = transform.TransformPoint(new Vector3(voxelSpaceBound.min.x * 2.2f, voxelSpaceBound.min.y * 2.2f, voxelSpaceBound.min.z * 2.2f));
                VoxelObject voxelObject = go.AddComponent<VoxelObject>();
                voxelObject.chunkSize = voxelSpaceBound.size + Vector3Int.one;
                voxelObject.chunk = new VoxelChunk(Vector3Int.zero, voxelSpaceBound.size + Vector3Int.one, chunk.voxelScale, new SimpleDataStructure());
                voxelObject.chunk.voxels.SetVoxels(regionVoxels);
                voxelObject.material = material;
                voxelObject.original = false;

                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                //rb.velocity = rbVel / 2;
            };
            GameObject.Destroy(transform.gameObject);
        }
    }

    public static void SeparateIslands(VoxelChunk chunk, Transform transform, Material material, Vector3 rbVel, Dictionary<Vector3Int, Voxel> fractureVoxels) {
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

    }

}