using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.Core.MeshProviders;

namespace VoxelMaster.Core.Rendering {
    class ChunkRenderer {

        Dictionary<Vector3Int, VoxelChunk> chunks;
        MarchingCubesGPU meshGenerator = new MarchingCubesGPU ();
        Material material;

        Vector3Int[] neighbourOffsets = new Vector3Int[26];

        public ChunkRenderer (Dictionary<Vector3Int, VoxelChunk> chunks, Material material) {
            this.chunks = chunks;

            this.material = material;
            GenerateOffsets ();

        }

        private void GenerateOffsets () {
            int cornerIndex = 0;
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++) {
                        if (x == 0 && y == 0 && z == 0) continue;
                        neighbourOffsets[cornerIndex++] = new Vector3Int (x, y, z);
                    }
        }

        public void Render () {
            foreach (KeyValuePair<Vector3Int, Mesh> item in previewMeshes) {
                Graphics.DrawMesh (item.Value, item.Key * 16, Quaternion.identity, material, 0);
            }
        }

        public void GenerateMeshes (Vector3Int viewerCoordinates) {

        }
        private Dictionary<Vector3Int, Mesh> previewMeshes = new Dictionary<Vector3Int, Mesh> ();
        private Dictionary<Vector3Int, MeshCollider> colliders = new Dictionary<Vector3Int, MeshCollider> ();
        private void OnChunkMesh (MeshGenerationResult res) {

        }

        public void RequestMesh (Vector3Int coord) {
            var mesh = meshGenerator.GenerateMesh (chunks[coord]);
            previewMeshes[coord] = mesh.BuildMesh ();
        }
    }
}