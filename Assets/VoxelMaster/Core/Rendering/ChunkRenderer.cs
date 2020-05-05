using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.Core.MeshProviders;

namespace VoxelMaster.Core.Rendering {
    public class ChunkRenderer {

        public static ChunkRenderer instance;

        private Dictionary<Vector3Int, VoxelChunk> chunks;
        private MarchingCubesGPU meshGenerator = new MarchingCubesGPU ();
        Material material;

        private Vector3Int[] neighbourOffsets = new Vector3Int[26];

        public ChunkRenderer (Dictionary<Vector3Int, VoxelChunk> chunks, Material material) {
            instance = this;
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

        public Mesh GetChunkMesh (Vector3Int coord) {
            return previewMeshes[coord];
        }

        public void RequestMesh (Vector3Int coord) {
            var chunk = chunks[coord];
            if (previewMeshes.ContainsKey (coord)) {
                AddPositiveNeighborEdges (chunk);
            }
            var mesh = meshGenerator.GenerateMesh (chunk);
            previewMeshes[coord] = mesh.BuildMesh ();
        }
        private void AddPositiveNeighborEdges (VoxelChunk chunk) {
            for (int side = 0; side < 3; side++) {
                if (!chunks.ContainsKey (chunk.coords + GetNeighborCoordOffset (side))) continue;
                VoxelChunk neighborChunk = chunks[chunk.coords + GetNeighborCoordOffset (side)];

                for (int u = 0; u < chunk.size.x; u++)
                    for (int v = 0; v < chunk.size.y; v++) {
                        Vector3Int voxelCoord = new Vector3Int (
                            positiveOrientation[side][0].x * (chunk.size.x - 1) + u * positiveOrientation[side][1].x + v * positiveOrientation[side][2].x,
                            positiveOrientation[side][0].y * (chunk.size.y - 1) + u * positiveOrientation[side][1].y + v * positiveOrientation[side][2].y,
                            positiveOrientation[side][0].z * (chunk.size.z - 1) + u * positiveOrientation[side][1].z + v * positiveOrientation[side][2].z
                        );

                        Vector3Int neighborVoxelCoord = new Vector3Int (
                            negativeOrientation[side][0].x * (chunk.size.x - 1) + u * negativeOrientation[side][1].x + v * negativeOrientation[side][2].x,
                            negativeOrientation[side][0].y * (chunk.size.y - 1) + u * negativeOrientation[side][1].y + v * negativeOrientation[side][2].y,
                            negativeOrientation[side][0].z * (chunk.size.z - 1) + u * negativeOrientation[side][1].z + v * negativeOrientation[side][2].z
                        );
                        Voxel neighborVoxel = neighborChunk.voxels.GetVoxel (neighborVoxelCoord);

                        chunk.voxels.SetVoxel (voxelCoord, neighborVoxel);
                    }
            }
        }

        private Vector3Int GetNeighborCoordOffset (int side) {
            switch (side) {
                case 0:
                    return Vector3Int.right;
                case 1:
                    return Vector3Int.up;
                case 2:
                    return forward;
                default:
                    return Vector3Int.zero;
            }
        }

        private static Vector3Int forward = new Vector3Int (0, 0, 1);

        //(cell_origin_shift, U_direction, V_direction, W_direction)
        private static readonly Vector3Int[][] positiveOrientation = new Vector3Int[][] {
            new Vector3Int[] { new Vector3Int (1, 0, 0), new Vector3Int (0, 0, 1), new Vector3Int (0, 1, 0), new Vector3Int (-1, 0, 0) }, //X+
            new Vector3Int[] { new Vector3Int (0, 1, 0), new Vector3Int (1, 0, 0), new Vector3Int (0, 0, 1), new Vector3Int (0, -1, 0) }, //Y+
            new Vector3Int[] { new Vector3Int (1, 0, 1), new Vector3Int (-1, 0, 0), new Vector3Int (0, 1, 0), new Vector3Int (0, 0, -1) } //Z+
        };

        private static readonly Vector3Int[][] reverseOrientation = new Vector3Int[][] {
            new Vector3Int[] { new Vector3Int (0, 0, 1), new Vector3Int (0, 0, -1), new Vector3Int (0, 1, 0), new Vector3Int (1, 0, 0) }, //X-
            new Vector3Int[] { new Vector3Int (0, 0, 1), new Vector3Int (1, 0, 0), new Vector3Int (0, 0, -1), new Vector3Int (0, 1, 0) }, //Y-
            new Vector3Int[] { new Vector3Int (1, 0, 1), new Vector3Int (-1, 0, 0), new Vector3Int (0, 1, 0), new Vector3Int (0, 0, -1) }, //Z-
        };

        private static readonly Vector3Int[][] negativeOrientation = new Vector3Int[][] {
            // new Vector3Int[] { new Vector3Int (0, 0, 1), new Vector3Int (0, 0, -1), new Vector3Int (0, 1, 0), new Vector3Int (1, 0, 0) }, //X-
            // new Vector3Int[] { new Vector3Int (0, 0, 1), new Vector3Int (1, 0, 0), new Vector3Int (0, 0, -1), new Vector3Int (0, 1, 0) }, //Y-
            // new Vector3Int[] { new Vector3Int (0, 0, 0), new Vector3Int (1, 0, 0), new Vector3Int (0, 1, 0), new Vector3Int (0, 0, 1) }, //Z-
            new Vector3Int[] { new Vector3Int (0, 0, 0), new Vector3Int (0, 0, 1), new Vector3Int (0, 1, 0), new Vector3Int (-1, 0, 0) }, //X-
            new Vector3Int[] { new Vector3Int (0, 0, 0), new Vector3Int (1, 0, 0), new Vector3Int (0, 0, 1), new Vector3Int (0, -1, 0) }, //Y-
            new Vector3Int[] { new Vector3Int (1, 0, 0), new Vector3Int (-1, 0, 0), new Vector3Int (0, 1, 0), new Vector3Int (0, 0, -1) } //Z-
        };
    }
}