using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        Queue<Vector3Int> chunkGenerationQueue = new Queue<Vector3Int> ();
        Queue<Vector3Int> generatedChunkQueue = new Queue<Vector3Int> ();

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

            new Thread (new ThreadStart (delegate {
                while (true) {
                    if (chunkGenerationQueue.Count > 0) {
                        var coord = chunkGenerationQueue.Dequeue ();
                        RequestChunk (coord);
                    }
                    Thread.Sleep (6);
                }
            })).Start ();
        }

        void ExpandChunkGeneration () {
            chunkGenerationQueue.Clear ();
            for (int y = -viewerRadius / 6; y < viewerRadius / 3; y++) {

                // The following is a spiral algorithm inspired by a StackOverflow post
                // https://stackoverflow.com/questions/398299/looping-in-a-spiral

                int size = viewerRadius;
                int x = 0, z = 0, dx = 0, dz = -1;
                int t = size;
                int maxI = t * t;

                for (int i = 0; i < maxI; i++) {
                    if ((-size / 2 <= x) && (x <= size / 2) && (-size / 2 <= z) && (z <= size / 2)) {
                        var coord = viewerCoordinates + new Vector3Int (x, y, z);
                        if (!chunkDictionary.ContainsKey (coord)) {
                            chunkGenerationQueue.Enqueue (coord);
                        }
                    }

                    if ((x == z) || ((x < 0) && (x == -z)) || ((x > 0) && (x == 1 - z))) {
                        t = dx;
                        dx = -dz;
                        dz = t;
                    }
                    x += dx;
                    z += dz;
                }
            }

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
                Util.Int_floor_division (coord.x, (chunkSize)),
                Util.Int_floor_division (coord.y, (chunkSize)),
                Util.Int_floor_division (coord.z, (chunkSize))
            );
            var voxelCoordInChunk = new Vector3Int (
                coord.x % (chunkSize),
                coord.y % (chunkSize),
                coord.z % (chunkSize)
            );

            if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize;
            if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize;
            if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize;

            // if (!chunkDictionary.ContainsKey (chunkCoord)) return new Voxel { density = 0 };

            return chunkDictionary[chunkCoord][voxelCoordInChunk];
        }
        private void SetVoxel (Vector3Int coord, Voxel voxel) {
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

            if (!chunkDictionary.ContainsKey (chunkCoord)) throw new IndexOutOfRangeException ();
            chunkDictionary[chunkCoord][voxelCoordInChunk] = voxel;
        }

        void Update () {
            UpdateViewerCoordinates ();
            if (viewerCoordinatesChanged) {
                ExpandChunkGeneration ();
            }

            if (generatedChunkQueue.Count > 0) {
                var coord = generatedChunkQueue.Dequeue ();
                chunkRenderer.RequestMesh (coord);
                CreateCollisionObject (coord, chunkRenderer.GetChunkMesh (coord));
            }

            chunkRenderer.Render ();
        }

        void ChunkGenerationThread () {

        }

        private async void RequestChunk (Vector3Int coord) {
            foreach (var provider in chunkProviders) {
                if (await provider.HasChunk (coord) == false) continue;
                provider.RequestChunk (coord, (chunk) => {
                    AddChunk (coord, chunk);
                    if (chunk.hasSolids) {
                        generatedChunkQueue.Enqueue (coord);
                    }
                });

                break;
            }
        }

        private void AddChunk (Vector3Int coord, VoxelChunk chunk) {
            chunkDictionary.Add (coord, chunk);
        }

        public Dictionary<Vector3Int, GameObject> gameObjects = new Dictionary<Vector3Int, GameObject> ();

        public void CreateCollisionObject (Vector3Int coord, Mesh mesh) {
            GameObject go = new GameObject ($"Chunk {coord}", typeof (MeshCollider));
            go.transform.position = coord * chunkSize;
            if (mesh != null) {
                go.GetComponent<MeshCollider> ().sharedMesh = mesh;
            }
            gameObjects.Add (coord, go);
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