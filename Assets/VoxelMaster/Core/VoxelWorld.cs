﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using static ThreadedMeshProvider;

// [ExecuteInEditMode]
public class VoxelWorld : MonoBehaviour, IVoxelData {

    #region Parameters
    public float voxelScale = 1;
    public float isoLevel = 0;
    public int chunkSize = 16;
    #endregion

    private WorldGenerator worldGenerator;
    private MeshGeneratorSettings meshGeneratorSettings;
    private ThreadedMeshProvider meshProvider;
    private Octree chunks = new Octree (16, 9);
    // private Dictionary<Vector3Int, Mesh> chunkMeshes = new Dictionary<Vector3Int, Mesh> ();
    private List<VoxelChunk> dirtChunks = new List<VoxelChunk> ();
    private List<VoxelMaterial> _materials = new List<VoxelMaterial> ();
    public List<VoxelMaterial> materials { get => new List<VoxelMaterial> (_materials); private set { _materials = value; } }

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

    private float viewDistance = 300;
    public Transform viewer;
    public Vector3Int viewerCoordinates;
    public int generationRadius = 3;

    [HideInInspector] public String dataStructureType;
    [HideInInspector] public String meshGeneratorType;

    new MeshCollider collider;

    #region Debug variables
    public Material material;
    #endregion

    private float SignedDistanceSphere (Vector3 pos, Vector3 center, float radius) {
        return Vector3.Distance (pos, center) - radius;
    }
    void Start () {

        worldGenerator = new WorldGenerator (new WorldGeneratorSettings {
            baseHeight = 0,
                heightAmplifier = 20,
                noiseScale = 1f,
                seed = 45432345,
                voxelScale = voxelScale
        });

        meshGeneratorSettings = new MeshGeneratorSettings {
            chunkSize = chunkSize,
            voxelScale = voxelScale,
            isoLevel = isoLevel
        };

        meshProvider = new ThreadedMeshProvider (Util.CreateInstance<VoxelMeshGenerator> (meshGeneratorType), meshGeneratorSettings);

        collider = new GameObject ("Terrain collider").AddComponent<MeshCollider> ();

        UpdateViewerCoordinates ();
        AddChunk (viewerCoordinates);
        ExpandChunkGeneration ();

    }

    bool viewerCoordinatesChanged = false;
    void UpdateViewerCoordinates () {
        viewerCoordinatesChanged = false;
        int targetChunkX = Int_floor_division ((int) viewer.position.x, chunkSize);
        int targetChunkY = Int_floor_division ((int) viewer.position.y, chunkSize);
        int targetChunkZ = Int_floor_division ((int) viewer.position.z, chunkSize);
        var newViewerCoordinates = new Vector3Int (targetChunkX, targetChunkY, targetChunkZ);
        if (newViewerCoordinates != viewerCoordinates) viewerCoordinatesChanged = true;
        viewerCoordinates = newViewerCoordinates;
    }

    void Update () {

        UpdateViewerCoordinates ();
        if (viewerCoordinatesChanged) {
            // ExpandChunkGeneration ();
            // AddChunk (viewerCoordinates);
            // GenerateTerrainMeshes ();
        }

        worldGenerator.MainThreadUpdate ();
        meshProvider.MainThreadUpdate ();

        RenderChunks ();
        // UpdateCollisionMeshes ();
        // if (!chunks.ContainsKey (Vector3Int.zero)) {
        //     AddChunk (new Vector3Int (0, 0, 0));
        // }
        // return;

    }

    private void GenerateTerrainMeshes () {
        previewMeshes.Clear ();

        // var currentNodeLocation = chunks.GetNodeIndexAtCoord (viewerCoordinates);
        // var parentNodeLocation = currentNodeLocation >> 3;

        // var lod0ChunkNodes = chunks.GetLeafChildren (parentNodeLocation);

        // foreach (var node in lod0ChunkNodes) {
        //     Voxel[] voxels = new Voxel[(chunkSize + 3) * (chunkSize + 3) * (chunkSize + 3)];

        //     meshProvider.RequestChunkMesh (new MeshGenerationRequest {
        //         locationCode = node.locationCode,
        //             voxels = ExtractVoxels ((node.chunk.coords * chunkSize) - Vector3Int.one, 0),
        //             size = chunkSize + 3,
        //             voxelScale = 1f,
        //             callback = OnChunkMesh
        //     });
        // }

        var currentNodeLocation = chunks.GetNodeIndexAtCoord (viewerCoordinates);
        uint lastNodeLocation = 0;
        for (int i = 0; i < 3; i++) {
            lastNodeLocation = currentNodeLocation;
            currentNodeLocation = currentNodeLocation >> 3;

            var lod0ChunkNodes = chunks.GetChildren (currentNodeLocation);
            foreach (var node in lod0ChunkNodes) {
                if (i != 0 && node.locationCode == lastNodeLocation) continue;
                Voxel[] voxels = new Voxel[(chunkSize + 3) * (chunkSize + 3) * (chunkSize + 3)];

                var extractStart = new Vector3Int (
                    (int) node.bounds.min.x,
                    (int) node.bounds.min.y,
                    (int) node.bounds.min.z
                ) - Vector3Int.one;

                meshProvider.RequestChunkMesh (new MeshGenerationRequest {
                    locationCode = node.locationCode,
                        voxels = ExtractVoxels (extractStart, i),
                        size = chunkSize + 3,
                        voxelScale = 1f * (1 << i),
                        callback = OnChunkMesh
                });
            }

        }
    }

    private Voxel[] ExtractVoxels (Vector3Int startVoxelCoord, int lod) {
        int size = (chunkSize + 3);
        Voxel[] voxels = new Voxel[size * size * size];
        for (byte z = 0; z < size; z += 1)
            for (byte y = 0; y < size; y += 1)
                for (byte x = 0; x < size; x += 1) {
                    var coord = new Vector3Int (x, y, z);
                    voxels[Util.Map3DTo1D (coord, size)] = GetVoxel (startVoxelCoord + coord * (1 << lod));
                }
        return voxels;
    }

    private Dictionary<uint, Mesh> previewMeshes = new Dictionary<uint, Mesh> ();
    private void OnChunkMesh (MeshGenerationResult res) {
        previewMeshes[res.locationCode] = res.meshData.BuildMesh ();
    }

    private void ExpandChunkGeneration () {
        for (int z = -generationRadius / 2; z < generationRadius / 2; z++)
            for (int y = -generationRadius / 2; y < generationRadius / 2; y++)
                for (int x = -generationRadius / 2; x < generationRadius / 2; x++) {
                    AddChunk (viewerCoordinates + new Vector3Int (x, y, z));
                }
        // foreach (var chunk in chunks.Values.ToArray ()) {
        //     bool allNeighboursHaveData = true;
        //     for (int z = -1; z <= 1; z++)
        //         for (int y = -1; y <= 1; y++)
        //             for (int x = -1; x <= 1; x++) {
        //                 if (x == 0 && y == 0 && z == 0) continue;
        //                 var neighbourCoords = new Vector3Int (
        //                     chunk.coords.x + x,
        //                     chunk.coords.y + y,
        //                     chunk.coords.z + z
        //                 );
        //                 if (!chunks.ContainsKey (neighbourCoords)) {
        //                     if (Vector3Int.Distance (viewerCoordinates, neighbourCoords) <= generationRadius) {
        //                         AddChunk (neighbourCoords);
        //                     }
        //                     allNeighboursHaveData = false;
        //                 } else {
        //                     if (!chunks[neighbourCoords].hasData) allNeighboursHaveData = false;
        //                 }
        //             }
        //     if (!chunk.hasMesh && allNeighboursHaveData) chunk.SetLod (0);
        // }
    }

    void RenderChunks () {
        // foreach (KeyValuePair<Vector3Int, VoxelChunk> entry in chunks) {
        //     var pos = new Vector3 (
        //         entry.Key.x * chunkSize,
        //         entry.Key.y * chunkSize,
        //         entry.Key.z * chunkSize
        //     );
        //     if (entry.Value.GetCurrentMesh () != null)
        //         Graphics.DrawMesh (entry.Value.GetCurrentMesh (), pos, Quaternion.identity, material, 0);
        // }

        foreach (KeyValuePair<uint, Mesh> entry in previewMeshes) {
            var meshNode = chunks.GetNode (entry.Key);
            Graphics.DrawMesh (entry.Value, meshNode.bounds.min, Quaternion.identity, material, 0);

        }
    }

    void UpdateCollisionMeshes () {

        // int targetChunkX = Int_floor_division ((int) viewer.position.x, chunkSize);
        // int targetChunkY = Int_floor_division ((int) viewer.position.y, chunkSize);
        // int targetChunkZ = Int_floor_division ((int) viewer.position.z, chunkSize);

        // var coord = new Vector3Int (targetChunkX, targetChunkY, targetChunkZ);
        // if (chunks.ContainsKey (coord))
        //     collider.sharedMesh = chunks[coord].GetCurrentMesh ();

    }

    private int Int_floor_division (int value, int divider) {
        int q = value / divider;
        if (value % divider < 0) return q - 1;
        else return q;
    }

    private Voxel GetVoxel (Vector3Int coord) {

        var chunk = new Vector3Int (
            Int_floor_division (coord.x, (chunkSize - 1)),
            Int_floor_division (coord.y, (chunkSize - 1)),
            Int_floor_division (coord.z, (chunkSize - 1))
        );
        var voxelCoordInChunk = new Vector3Int (
            coord.x % (chunkSize - 1),
            coord.y % (chunkSize - 1),
            coord.z % (chunkSize - 1)
        );

        if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize - 1;
        if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize - 1;
        if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize - 1;

        if (chunks.GetChunkAtCoord (chunk) == null) return new Voxel { density = 0 };

        return chunks.GetChunkAtCoord (chunk) [voxelCoordInChunk];
    }
    private void SetVoxel (Vector3Int coord, Voxel voxel) {
        // var chunk = new Vector3Int (
        //     Int_floor_division (coord.x, (chunkSize - 1)),
        //     Int_floor_division (coord.y, (chunkSize - 1)),
        //     Int_floor_division (coord.z, (chunkSize - 1))
        // );
        // var voxelCoordInChunk = new Vector3Int (
        //     coord.x % (chunkSize - 1),
        //     coord.y % (chunkSize - 1),
        //     coord.z % (chunkSize - 1)
        // );

        // if (voxelCoordInChunk.x < 0) voxelCoordInChunk.x += chunkSize - 1;
        // if (voxelCoordInChunk.y < 0) voxelCoordInChunk.y += chunkSize - 1;
        // if (voxelCoordInChunk.z < 0) voxelCoordInChunk.z += chunkSize - 1;

        // if (!chunks.ContainsKey (chunk)) throw new IndexOutOfRangeException ();
        // chunks[chunk][voxelCoordInChunk] = voxel;
    }

    public void AddChunk (Vector3Int pos) {
        if (chunks.GetNodeIndexAtCoord (pos) != 0) return;
        var chunkVoxels = Util.CreateInstance<VoxelDataStructure> (dataStructureType);
        var chunk = new VoxelChunk (pos, chunkSize, voxelScale, chunkVoxels);
        chunk.voxelWorld = this;
        chunks.AddChunk (pos, chunk);

        worldGenerator.RequestChunkData (chunk, OnChunkData);
    }

    private void OnChunkData (VoxelChunk chunk) {
        chunk.setHasData ();
    }

    private bool AllNeighboursHaveData (VoxelChunk chunk) {
        // for (int z = -1; z <= 1; z++)
        //     for (int y = -1; y <= 1; y++)
        //         for (int x = -1; x <= 1; x++) {
        //             if (x == 0 && y == 0 && z == 0) continue;
        //             var coord = chunk.coords + new Vector3Int (x, y, z);
        //             if (!chunks.ContainsKey (coord) || chunks[coord].hasData == false) {
        //                 return false;
        //             }
        //         }
        return true;
    }

    private void ChunkForEachNeighbour (VoxelChunk chunk, Action<VoxelChunk> cb) {
        // for (int z = -1; z <= 1; z++)
        //     for (int y = -1; y <= 1; y++)
        //         for (int x = -1; x <= 1; x++) {
        //             if (x == 0 && y == 0 && z == 0) continue;
        //             var coord = chunk.coords + new Vector3Int (x, y, z);
        //             if (chunks.ContainsKey (coord)) {
        //                 cb.Invoke (chunks[coord]);
        //             }
        //         }
    }

    public void RemoveChunk (Vector3Int pos) {
        throw new NotImplementedException ();
    }
    public VoxelChunk GetChunk (Vector3Int pos) {
        throw new NotImplementedException ();
    }
    public void AddDensity (Vector3Int pos, float[][][] densities) {
        throw new NotImplementedException ();
    }
    public void SetDensity (Vector3Int pos, float[][][] densities) {
        throw new NotImplementedException ();
    }
    public void RemoveDensity (Vector3Int pos, float[][][] densities) {
        throw new NotImplementedException ();
    }
    public VoxelMaterial GetMaterial (Vector3 pos) {
        throw new NotImplementedException ();
    }
    public void SetMaterial (Vector3 pos, byte materialIndex) {
        throw new NotImplementedException ();
    }
    public void SetMaterialInRadius (Vector3 pos, float radius, byte materialIndex) {
        throw new NotImplementedException ();
    }

    void DrawNeighbour (OctreeNode from, byte axis, bool direction = true) {
        DrawNodeIfExists (chunks.RelativeLocation (from.locationCode, axis, direction));
    }

    void DrawNodeIfExists (uint location) {
        var positiveNeighbour = chunks.GetNode (location);
        if (positiveNeighbour != null)
            Gizmos.DrawWireCube (positiveNeighbour.bounds.center, positiveNeighbour.bounds.size);
    }

    Color idleColor = new Color (1, 1, 1, .05f);
    void OnDrawGizmos () {
        if (chunks == null) return;

        // chunks.DrawLeafNodes ();

        var currentNode = chunks.GetNode (chunks.GetNodeIndexAtCoord (viewerCoordinates));
        if (currentNode != null) {

            Gizmos.DrawWireCube (currentNode.bounds.center, currentNode.bounds.size);

            Gizmos.color = Color.green;
            DrawNodeIfExists (currentNode.locationCode >> 3);
            Gizmos.color = Color.yellow;
            DrawNodeIfExists (currentNode.locationCode >> 6);
            Gizmos.color = Color.red;
            DrawNodeIfExists (currentNode.locationCode >> 9);

            // Gizmos.color = Color.red;
            // DrawNeighbour (currentNode, 0b001);
            // Gizmos.color = Color.blue;
            // DrawNeighbour (currentNode, 0b010);
            // Gizmos.color = Color.green;
            // DrawNeighbour (currentNode, 0b100);
            // Gizmos.color = Color.red;
            // DrawNeighbour (currentNode, 0b001, false);
            // Gizmos.color = Color.blue;
            // DrawNeighbour (currentNode, 0b010, false);
            // Gizmos.color = Color.green;
            // DrawNeighbour (currentNode, 0b100, false);

        }

    }
}