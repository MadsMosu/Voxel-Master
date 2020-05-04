using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.Chunk.ChunkProviders;
using VoxelMaster.Core;
using VoxelMaster.Core.Rendering;
using VoxelMaster.WorldGeneration;

namespace VoxelMaster {
    //[ExecuteInEditMode]
    public class VoxelWorld : MonoBehaviour, IVoxelData {

        public int chunkSize = 16;

        List<ChunkDataProvider> chunkProviders = new List<ChunkDataProvider> ();
        ChunkSerializer chunkSerializer;
        public Dictionary<Vector3Int, VoxelChunk> chunkDictionary = new Dictionary<Vector3Int, VoxelChunk> ();

        Queue<VoxelChunk> generatedChunkQueue = new Queue<VoxelChunk> ();

        public Transform viewer;
        Vector3Int viewerCoordinates = Vector3Int.zero;
        public int viewerRadius = 8;
        bool viewerCoordinatesChanged = false;

        ChunkRenderer chunkRenderer;
        public Material material;

        void Start () {
            chunkRenderer = new ChunkRenderer (chunkDictionary, material);
            chunkSerializer = new ChunkSerializer ("world");
            //chunkProviders.Add(new FileChunkDataProvider("world"));
            chunkProviders.Add (new GeneratorChunkDataProvider (new WorldGeneratorSettings {
                baseHeight = 2,
                    heightAmplifier = 2,
                    noiseScale = 1,
                    seed = 23342,
                    voxelScale = 1
            }));
        }

        void ExpandChunkGeneration () {
            List<Vector3Int> toGenCoords = new List<Vector3Int> ();
            for (int z = -viewerRadius / 2; z < viewerRadius / 2; z++)
                for (int y = -viewerRadius / 6; y < viewerRadius / 3; y++)
                    for (int x = -viewerRadius / 2; x < viewerRadius / 2; x++) {
                        if (x == 0 && y == 0 && z == 0) continue;
                        var coord = viewerCoordinates + new Vector3Int (x, y, z);
                        if (!chunkDictionary.ContainsKey (coord)) {
                            toGenCoords.Add (coord);
                        }
                    }
            toGenCoords.OrderBy (c => Vector3Int.Distance (c, viewerCoordinates)).ToList ().ForEach (c => RequestChunk (c));
        }

        void UpdateViewerCoordinates () {
            viewerCoordinatesChanged = false;
            int targetChunkX = Util.Int_floor_division ((int) viewer.position.x, chunkSize);
            int targetChunkY = Util.Int_floor_division ((int) viewer.position.y, chunkSize);
            int targetChunkZ = Util.Int_floor_division ((int) viewer.position.z, chunkSize);
            var newViewerCoordinates = new Vector3Int (targetChunkX, targetChunkY, targetChunkZ);
            viewerCoordinatesChanged = newViewerCoordinates != viewerCoordinates;
            viewerCoordinates = newViewerCoordinates;
        }

        Queue<Vector3Int> chunksNeedMesh = new Queue<Vector3Int> ();

        public Voxel this [Vector3 v] {
            get => this [(int) v.x, (int) v.y, (int) v.z];
            set => this [(int) v.x, (int) v.y, (int) v.z] = value;
        }
        public Voxel this [Vector3Int v] {
            get => GetVoxel (v);
            set => SetVoxel (v, value);
        }
        public Voxel this [int x, int y, int z] {
            get => this [new Vector3Int (x, y, z)];
            set => this [new Vector3Int (x, y, z)] = value;
        }

        private Voxel GetVoxel (Vector3Int coord) {

            var chunkCoord = new Vector3Int (
                Util.Int_floor_division (coord.x, (chunkSize - 1)),
                Util.Int_floor_division (coord.y, (chunkSize - 1)),
                Util.Int_floor_division (coord.z, (chunkSize - 1))
            );
            var voxelCoordInChunk = new Vector3Int (
                coord.x % (chunkSize - 1),
                coord.y % (chunkSize - 1),
                coord.z % (chunkSize - 1)
            );

            if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize - 1;
            if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize - 1;
            if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize - 1;

            if (!chunkDictionary.ContainsKey (chunkCoord)) return new Voxel { density = 0 };

            return chunkDictionary[chunkCoord][voxelCoordInChunk];
        }
        private void SetVoxel (Vector3Int coord, Voxel voxel) {
            var chunk = new Vector3Int (
                Util.Int_floor_division (coord.x, (chunkSize - 1)),
                Util.Int_floor_division (coord.y, (chunkSize - 1)),
                Util.Int_floor_division (coord.z, (chunkSize - 1))
            );
            var voxelCoordInChunk = new Vector3Int (
                coord.x % (chunkSize - 1),
                coord.y % (chunkSize - 1),
                coord.z % (chunkSize - 1)
            );

            if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize - 1;
            if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize - 1;
            if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize - 1;

            if (!chunkDictionary.ContainsKey (chunk)) throw new IndexOutOfRangeException ();
            chunkDictionary[chunk][voxelCoordInChunk] = voxel;
        }

        void Update () {
            UpdateViewerCoordinates ();
            if (viewerCoordinatesChanged) {
                Task.Run (delegate {
                    ExpandChunkGeneration ();
                });
            }

            if (chunksNeedMesh.Count > 0) {
                var coord = chunksNeedMesh.Dequeue ();
                chunkRenderer.RequestMesh (coord);
            }

            chunkRenderer.Render ();
        }

        private async void RequestChunk (Vector3Int coord) {
            foreach (var provider in chunkProviders) {
                if (await provider.HasChunk (coord) == false) continue;
                var chunk = await provider.RequestChunk (coord);
                AddChunk (coord, chunk);
                if (chunk.hasSolids || chunk.needsUpdate) {
                    lock (chunksNeedMesh) {
                        chunksNeedMesh.Enqueue (chunk.coords);
                    }
                    chunk.needsUpdate = false;
                }

                //if (provider is GeneratorChunkDataProvider)
                //    chunkSerializer.SaveChunk(chunk);

                break;
            }
        }

        private void AddChunk (Vector3Int coord, VoxelChunk chunk) {
            chunkDictionary.Add (coord, chunk);
        }

        private void OnDrawGizmosSelected () {
            // lock (chunkDictionary) {
            //     foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunkDictionary) {
            //         var pos = entry.Key * chunkSize;
            //         Gizmos.color = entry.Value.hasSolids ? Color.blue : Color.white;
            //         Gizmos.DrawWireCube (pos - (Vector3.one * (-chunkSize / 2)), chunkSize * Vector3.one);
            //     }
            // }
        }

    }
}