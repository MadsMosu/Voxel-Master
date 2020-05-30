using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.Core.MeshProviders;
using VoxelMaster.WorldGeneration;

namespace VoxelMaster.Core.Rendering {
    class OctreeRenderer {

        public static Voxel[] testArrayVoxels;

        Octree<Vector3> octree;
        VoxelWorld world;
        MarchingCubesGPU meshGenerator = new MarchingCubesGPU();
        Material material;

        Vector3Int[] neighbourOffsets = new Vector3Int[26];

        Queue<uint> generationQueue = new Queue<uint>();
        List<uint> destructionQueue = new List<uint>();
        Dictionary<uint, List<VoxelChunk>> regionChunkMap = new Dictionary<uint, List<VoxelChunk>>();

        private WorldGeneratorSettings settings;
        private WorldHeightmapGenerator generator;

        private Dictionary<uint, GameObject> regions = new Dictionary<uint, GameObject>();

        private const int CHUNK_SIZE = 16;

        public OctreeRenderer(Octree<Vector3> octree, VoxelWorld world, Material material, WorldGeneratorSettings settings) {
            this.octree = octree;
            this.world = world;
            var meshGeneratorSettings = new MeshGeneratorSettings {
                chunkSize = 16,
                voxelScale = 1,
                isoLevel = 0
            };

            this.material = material;
            this.settings = settings;
            generator = new WorldHeightmapGenerator(settings);

            DebugGUI.AddVariable("Region Generation Queue", () => generationQueue.Count);
            DebugGUI.AddVariable("Region Destruction Queue", () => destructionQueue.Count);

        }

        private bool HasRenderParent(uint child) {
            uint loc = child;
            while (loc > 1) {
                if (regions.ContainsKey(loc) && !generationQueue.Contains(child)) return true;
                loc >>= 3;
            }
            return false;
        }

        private bool CanRemoveRegion(uint node) {
            var parent = node << 3;
            if (HasRenderParent(node)) return true;
            var children = octree.GetChildren(node);
            if (children.All(c => regions.ContainsKey(c.locationCode))) return true;
            return false;
        }

        public void Update() {
            generationQueue.Clear();
            // regions.Clear ();

            var newLeafNodes = octree.GetLeafChildren(0b1).Select(n => n.locationCode).ToList();
            // Debug.Log (newLeafNodes.Count);
            foreach (var renderMesh in regions) {
                var key = renderMesh.Key;
                if (!newLeafNodes.Contains(key)) {
                    destructionQueue.Add(key);
                }
            }
            newLeafNodes.Where(n => !regions.ContainsKey(n)).OrderByDescending(n => n).ToList().ForEach(n => {
                generationQueue.Enqueue(n);
            });

        }

        public IEnumerator ProcessGenerationQueue() {
            while (true) {
                for (int i = 0; i < 1; i++) {
                    if (generationQueue.Count <= 0) break;
                    var n = octree.GetNode(generationQueue.Dequeue());
                    if (n == null) continue;
                    var nodeDepth = Octree<Vector3>.GetDepth(n.locationCode);
                    var voxels = GetNodeVoxels(n.locationCode);
                    var mesh = meshGenerator.GenerateMesh(voxels, Vector3Int.one * (world.chunkSize + 1), 1, 1 << (octree.GetMaxDepth() - nodeDepth)).BuildMesh();
                    if (regions.ContainsKey(n.locationCode)) {
                        var region = regions[n.locationCode];
                        region.GetComponent<MeshFilter>().mesh = mesh;
                        region.GetComponent<MeshCollider>().sharedMesh = mesh;
                    }
                    else {
                        regions[n.locationCode] = CreateCollisionObject(new Vector3Int((int)n.bounds.min.x, (int)n.bounds.min.y, (int)n.bounds.min.z), mesh);
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private Voxel[] GetNodeVoxels(uint nodeLocation) {
            var node = octree.GetNode(nodeLocation);
            var nodeDepth = Octree<Vector3>.GetDepth(nodeLocation);
            List<VoxelChunk> regionChunks = new List<VoxelChunk>();

            var result = new Voxel[(world.chunkSize + 1) * (world.chunkSize + 1) * (world.chunkSize + 1)];
            var chunkExtents = (1 << (octree.GetMaxDepth() - nodeDepth));

            int startingChunkX = Util.Int_floor_division((int)node.bounds.min.x, world.chunkSize);
            int startingChunkY = Util.Int_floor_division((int)node.bounds.min.y, world.chunkSize);
            int startingChunkZ = Util.Int_floor_division((int)node.bounds.min.z, world.chunkSize);

            var voxelIncrementer = 1 << (octree.GetMaxDepth() - nodeDepth);
            var chunkIncrementer = Mathf.Max(1, voxelIncrementer / world.chunkSize);



            for (int chunkX = 0; chunkX <= chunkExtents; chunkX += chunkIncrementer) {
                for (int chunkY = 0; chunkY <= chunkExtents; chunkY += chunkIncrementer) {
                    for (int chunkZ = 0; chunkZ <= chunkExtents; chunkZ += chunkIncrementer) {

                        int voxelXAmount = chunkX == chunkExtents ? 1 : world.chunkSize;
                        int voxelYAmount = chunkY == chunkExtents ? 1 : world.chunkSize;
                        int voxelZAmount = chunkZ == chunkExtents ? 1 : world.chunkSize;

                        if (world.chunkDictionary.ContainsKey(new Vector3Int(startingChunkX, startingChunkY, startingChunkZ) + new Vector3Int(chunkX, chunkY, chunkZ))) {

                            var chunk = world.chunkDictionary[new Vector3Int(startingChunkX, startingChunkY, startingChunkZ) + new Vector3Int(chunkX, chunkY, chunkZ)];
                            regionChunks.Add(chunk);

                            for (int vx = 0; vx < voxelXAmount; vx += voxelIncrementer)
                                for (int vy = 0; vy < voxelYAmount; vy += voxelIncrementer)
                                    for (int vz = 0; vz < voxelZAmount; vz += voxelIncrementer) {
                                        int x = (vx / voxelIncrementer) + (chunkX * (world.chunkSize / voxelIncrementer));
                                        int y = (vy / voxelIncrementer) + (chunkY * (world.chunkSize / voxelIncrementer));
                                        int z = (vz / voxelIncrementer) + (chunkZ * (world.chunkSize / voxelIncrementer));
                                        result[Util.Map3DTo1D(x, y, z, world.chunkSize + 1)] = chunk.voxels.GetVoxel(vx, vy, vz);
                                    }
                        }
                        //else {
                        //    for (int vx = 0; vx < voxelXAmount; vx += voxelIncrementer)
                        //        for (int vy = 0; vy < voxelYAmount; vy += voxelIncrementer)
                        //            for (int vz = 0; vz < voxelZAmount; vz += voxelIncrementer) {

                        //                var density = generator.SampleDensity(
                        //                    (startingChunkX + chunkX) * CHUNK_SIZE + vx * settings.voxelScale,
                        //                    (startingChunkY + chunkY) * CHUNK_SIZE + vy * settings.voxelScale,
                        //                    (startingChunkZ + chunkZ) * CHUNK_SIZE + vz * settings.voxelScale
                        //                );

                        //                result[Util.Map3DTo1D(
                        //                    (vx / voxelIncrementer) + (chunkX * (world.chunkSize / voxelIncrementer)),
                        //                    (vy / voxelIncrementer) + (chunkY * (world.chunkSize / voxelIncrementer)),
                        //                    (vz / voxelIncrementer) + (chunkZ * (world.chunkSize / voxelIncrementer)),
                        //                    world.chunkSize + 1)] = new Voxel { density = density };

                        //            }
                        //}
                    }
                }
            }

            if (nodeLocation == 0b1) testArrayVoxels = result;
            regionChunkMap[nodeLocation] = regionChunks;
            return result;
        }

        public void Render() {
            for (int i = destructionQueue.Count - 1; i >= 0; i--) {
                var loc = destructionQueue[i];
                if (CanRemoveRegion(loc)) {
                    GameObject.Destroy(regions[loc]);
                    regions.Remove(loc);
                    regionChunkMap.Remove(loc);
                    destructionQueue.RemoveAt(i);
                }
            }

            foreach (var region in regionChunkMap) {
                if (region.Value.Any(c => c.dirty)) {
                    if (!generationQueue.Contains(region.Key)) {
                        generationQueue.Enqueue(region.Key);
                    }
                    region.Value.ForEach(c => c.dirty = false);
                }
            }
            // Debug.Log (generationQueue.Count ());
        }
        public GameObject CreateCollisionObject(Vector3Int coord, Mesh mesh) {
            GameObject go = new GameObject($"Chunk {coord}", typeof(MeshCollider), typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.position = coord;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.GetComponent<MeshRenderer>().material = material;

            return go;
        }

    }

}