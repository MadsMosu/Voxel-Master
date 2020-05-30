using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.Core.MeshProviders;

namespace VoxelMaster.Core.Rendering {
    class OctreeRenderer {

        public static Voxel[] testArrayVoxels;

        Octree<Vector3> octree;
        VoxelWorld world;
        MarchingCubesGPU meshGenerator = new MarchingCubesGPU ();
        Material material;

        Vector3Int[] neighbourOffsets = new Vector3Int[26];

        public OctreeRenderer (Octree<Vector3> octree, VoxelWorld world, Material material) {
            this.octree = octree;
            this.world = world;
            var meshGeneratorSettings = new MeshGeneratorSettings {
                chunkSize = 16,
                voxelScale = 1,
                isoLevel = 0
            };

            this.material = material;

        }

        public void Update () {
            renderMeshes.Clear ();
            octree.GetLeafChildren (0b1).ForEach (n => {
                if (renderMeshes.ContainsKey (n.locationCode)) return;
                var nodeDepth = Octree<Vector3>.GetDepth (n.locationCode);

                renderMeshes[n.locationCode] = meshGenerator.GenerateMesh (GetNodeVoxels (n.locationCode), Vector3Int.one * (world.chunkSize + 1), 1, 1 << (octree.GetMaxDepth () - nodeDepth)).BuildMesh ();

            });
        }

        private Voxel[] GetNodeVoxels (uint nodeLocation) {
            var node = octree.GetNode (nodeLocation);
            var nodeDepth = Octree<Vector3>.GetDepth (nodeLocation);

            var result = new Voxel[(world.chunkSize + 1) * (world.chunkSize + 1) * (world.chunkSize + 1)];
            var chunkExtents = (1 << (octree.GetMaxDepth () - nodeDepth));

            var startingChunkCoord = new Vector3Int (
                Util.Int_floor_division ((int) node.bounds.min.x, world.chunkSize),
                Util.Int_floor_division ((int) node.bounds.min.y, world.chunkSize),
                Util.Int_floor_division ((int) node.bounds.min.z, world.chunkSize)
            );

            var voxelIncrementer = 1 << (octree.GetMaxDepth () - nodeDepth);
            var chunkIncrementer = Mathf.Max (1, voxelIncrementer / world.chunkSize);

            for (int chunkX = 0; chunkX <= chunkExtents; chunkX += chunkIncrementer) {
                for (int chunkY = 0; chunkY <= chunkExtents; chunkY += chunkIncrementer) {
                    for (int chunkZ = 0; chunkZ <= chunkExtents; chunkZ += chunkIncrementer) {

                        int voxelXAmount = chunkX == chunkExtents ? 1 : world.chunkSize;
                        int voxelYAmount = chunkY == chunkExtents ? 1 : world.chunkSize;
                        int voxelZAmount = chunkZ == chunkExtents ? 1 : world.chunkSize;

                        if (world.chunkDictionary.ContainsKey (startingChunkCoord + new Vector3Int (chunkX, chunkY, chunkZ))) {

                            var chunk = world.chunkDictionary[startingChunkCoord + new Vector3Int (chunkX, chunkY, chunkZ)];

                            for (int vx = 0; vx < voxelXAmount; vx += voxelIncrementer)
                                for (int vy = 0; vy < voxelYAmount; vy += voxelIncrementer)
                                    for (int vz = 0; vz < voxelZAmount; vz += voxelIncrementer) {

                                        int voxelIndex = Util.Map3DTo1D (new Vector3Int (vx, vy, vz), world.chunkSize);
                                        result[Util.Map3DTo1D (new Vector3Int (
                                            (vx / voxelIncrementer) + (chunkX * (world.chunkSize / voxelIncrementer)),
                                            (vy / voxelIncrementer) + (chunkY * (world.chunkSize / voxelIncrementer)),
                                            (vz / voxelIncrementer) + (chunkZ * (world.chunkSize / voxelIncrementer))
                                        ), world.chunkSize + 1)] = chunk.voxels.GetVoxel (voxelIndex);

                                    }
                        } else {
                            for (int vx = 0; vx < voxelXAmount; vx += voxelIncrementer)
                                for (int vy = 0; vy < voxelYAmount; vy += voxelIncrementer)
                                    for (int vz = 0; vz < voxelZAmount; vz += voxelIncrementer) {
                                        result[Util.Map3DTo1D (new Vector3Int (
                                            (vx / voxelIncrementer) + (chunkX * (world.chunkSize / voxelIncrementer)),
                                            (vy / voxelIncrementer) + (chunkY * (world.chunkSize / voxelIncrementer)),
                                            (vz / voxelIncrementer) + (chunkZ * (world.chunkSize / voxelIncrementer))
                                        ), world.chunkSize + 1)] = new Voxel (-1);

                                    }
                        }
                    }
                }
            }

            if (nodeLocation == 0b1) testArrayVoxels = result;
            return result;
        }

        public void Render () {
            foreach (KeyValuePair<uint, Mesh> entry in renderMeshes) {
                var meshNode = octree.GetNode (entry.Key);
                Graphics.DrawMesh (entry.Value, meshNode.bounds.min, Quaternion.identity, material, 0);
            }
        }

        private Dictionary<uint, Mesh> renderMeshes = new Dictionary<uint, Mesh> ();
        private Dictionary<uint, MeshCollider> colliders = new Dictionary<uint, MeshCollider> ();
        private void OnChunkMesh (MeshGenerationResult res) {
            Debug.Log ($"Got mesh: {res.locationCode}");
            var mesh = res.meshData.BuildMesh ();
            renderMeshes[res.locationCode] = mesh;

            //MeshCollider collider;
            //if (colliders.ContainsKey(res.locationCode))
            //    collider = colliders[res.locationCode];
            //else
            //    colliders[res.locationCode] = new GameObject().AddComponent<MeshCollider>();

            //colliders[res.locationCode].sharedMesh = mesh;
            //colliders[res.locationCode].transform.position = octree.GetNode(res.locationCode).bounds.min;

        }

    }
}