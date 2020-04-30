using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoxelMaster.Core.MeshProviders;

namespace VoxelMaster.Core.Rendering {
    class OctreeRenderer  {

        Octree octree;
        ThreadedMeshProvider meshProvider;
        Material material;

        Vector3Int[] neighbourOffsets = new Vector3Int[26];

        public OctreeRenderer(Octree octree, IVoxelData volume, Material material) {
            this.octree = octree;
            var meshGeneratorSettings = new MeshGeneratorSettings {
                chunkSize = 16,
                voxelScale = 1,
                isoLevel = 0
            };

            this.material = material;

            meshProvider = new ThreadedMeshProvider(volume, new MarchingCubesEnhanced(), meshGeneratorSettings);
            GenerateOffsets();

        }

        private void GenerateOffsets() {
            int cornerIndex = 0;
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++) {
                        if (x == 0 && y == 0 && z == 0) continue;
                        neighbourOffsets[cornerIndex++] = new Vector3Int(x, y, z);
                    }
        }

        public void Render() {
            meshProvider.MainThreadUpdate();

            foreach (KeyValuePair<uint, Mesh> entry in previewMeshes) {
                var meshNode = octree.GetNode(entry.Key);
                Graphics.DrawMesh(entry.Value, meshNode.bounds.min, Quaternion.identity, material, 0);
            }
        }

        public void GenerateMeshes(Vector3Int viewerCoordinates) {
            HashSet<uint> lod0Nodes = new HashSet<uint>();
            HashSet<uint> lod1Nodes = new HashSet<uint>();
            HashSet<uint> lod2Nodes = new HashSet<uint>();


            var currentNodeLocation = octree.GetNodeIndexAtCoord(viewerCoordinates);
            lod0Nodes.Add(currentNodeLocation);
            lod2Nodes.Add(currentNodeLocation >> 9);

            for (int i = 0; i < neighbourOffsets.Length; i++) {
                lod0Nodes.Add(Octree.RelativeLeafNodeLocation(currentNodeLocation, neighbourOffsets[i]));
                lod1Nodes.Add(Octree.RelativeLeafNodeLocation(currentNodeLocation >> 3, neighbourOffsets[i]));
                lod2Nodes.Add(Octree.RelativeLeafNodeLocation(currentNodeLocation >> 6, neighbourOffsets[i]));
            }

            lod0Nodes.ToList().ForEach(code => {
                var node = octree.GetNode(code);
                if (node == null) return;
                var request = new MeshGenerationRequest {
                    origin = Util.FloorVector3(node.bounds.min),
                    locationCode = code,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1 << 0
                };
                meshProvider.RequestChunkMesh(request);
            });
            lod1Nodes.ToList().ForEach(code => {
                var node = octree.GetNode(code);
                if (node == null) return;
                meshProvider.RequestChunkMesh(new MeshGenerationRequest {
                    origin = Util.FloorVector3(node.bounds.min),
                    locationCode = code,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1 << 1
                });
            });
            lod2Nodes.ToList().ForEach(code => {
                var node = octree.GetNode(code);
                if (node == null) return;
                meshProvider.RequestChunkMesh(new MeshGenerationRequest {
                    origin = Util.FloorVector3(node.bounds.min),
                    locationCode = code,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1 << 2
                });
            });
        }
        private Dictionary<uint, Mesh> previewMeshes = new Dictionary<uint, Mesh>();
        private Dictionary<uint, MeshCollider> colliders = new Dictionary<uint, MeshCollider>();
        private void OnChunkMesh(MeshGenerationResult res) {
            var mesh = res.meshData.BuildMesh();
            previewMeshes[res.locationCode] = mesh;


            MeshCollider collider;
            if (colliders.ContainsKey(res.locationCode))
                collider = colliders[res.locationCode];
            else
                colliders[res.locationCode] = new GameObject().AddComponent<MeshCollider>();

            colliders[res.locationCode].sharedMesh = mesh;
            colliders[res.locationCode].transform.position = octree.GetNode(res.locationCode).bounds.min;

        }


    }
}
