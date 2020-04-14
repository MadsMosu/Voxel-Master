using System;
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

    private enum Sides {
        NegativeX = 0,
        PositiveX = 1,
        NegativeY = 2,
        PositiveY = 3,
        NegativeZ = 4,
        PositiveZ = 5
    }

    private float SignedDistanceSphere (Vector3 pos, Vector3 center, float radius) {
        return Vector3.Distance (pos, center) - radius;
    }

    Vector3Int[] neighbourOffsets = new Vector3Int[26];
    void Start () {
        int cornerIndex = 0;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++) {
                    if (x == 0 && y == 0 && z == 0) continue;
                    neighbourOffsets[cornerIndex++] = new Vector3Int (x, y, z);
                }

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
            GenerateTerrainMeshes ();
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

    HashSet<uint> lod0Nodes = new HashSet<uint> ();
    HashSet<uint> lod1Nodes = new HashSet<uint> ();
    HashSet<uint> lod2Nodes = new HashSet<uint> ();
    private void GenerateTerrainMeshes () {
        previewMeshes.Clear ();
        lod0Nodes.Clear ();
        lod1Nodes.Clear ();
        lod2Nodes.Clear ();

        var currentNodeLocation = chunks.GetNodeIndexAtCoord (viewerCoordinates);
        lod0Nodes.Add (currentNodeLocation);
        lod2Nodes.Add (currentNodeLocation >> 9);

        for (int i = 0; i < neighbourOffsets.Length; i++) {
            lod0Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation, neighbourOffsets[i]));
            lod1Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation >> 6, neighbourOffsets[i]));
            lod2Nodes.Add (Octree.RelativeLeafNodeLocation (currentNodeLocation >> 9, neighbourOffsets[i]));
            // uint lod0Code = chunks.RelativeLeafNodeLocation (currentNodeLocation, cornerOffsets[i]);
            // chunks.GetChildLocations (lod0Code >> 3).ForEach (c => lod0Nodes.Add (c));
            // chunks.GetChildLocations (lod0Code >> 6).ForEach (c => lod1Nodes.Add (c));
            // chunks.GetChildLocations (lod0Code >> 9).ForEach (c => lod2Nodes.Add (c));
        }

        lod0Nodes.ToList ().ForEach (code => {
            var nodeVoxels = ExtractVoxels (code, true);

            meshProvider.RequestChunkMesh (new MeshGenerationRequest {
                locationCode = code,
                    voxels = nodeVoxels,
                    size = chunkSize + 3,
                    voxelScale = 1f,
                    callback = OnChunkMesh,
                    step = 1
            });
        });

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

        // var currentNodeLocation = chunks.GetNodeIndexAtCoord (viewerCoordinates);
        // uint lastNodeLocation = 0;
        // for (int i = 0; i < 2; i++) {
        //     lastNodeLocation = currentNodeLocation;
        //     currentNodeLocation = currentNodeLocation >> 3;

        //     var lod0ChunkNodes = chunks.GetChildren (currentNodeLocation);
        //     foreach (var node in lod0ChunkNodes) {
        //         if (i != 0 && node.locationCode == lastNodeLocation) continue;

        //         int lodStep = 1 << i;
        //         int prevLodStep = 1 << (i - 1);
        //         int step = i > 0 ? prevLodStep : lodStep;
        //         int size = (chunkSize * lodStep) + (lodStep * 3);
        //         var extractStart = new Vector3Int (
        //             (int) node.bounds.min.x,
        //             (int) node.bounds.min.y,
        //             (int) node.bounds.min.z
        //         ) - new Vector3Int (lodStep, lodStep, lodStep);

        //         // var sides = new List<Sides> ();

        //         meshProvider.RequestChunkMesh (new MeshGenerationRequest {
        //             locationCode = node.locationCode,
        //                 voxels = ExtractVoxels (extractStart, size, step),
        //                 size = size,
        //                 step = lodStep,
        //                 voxelScale = 1f,
        //                 callback = OnChunkMesh
        //         });
        //     }
        // }
    }

    static Vector3Int vector3IntForward = new Vector3Int (0, 0, 1);
    private readonly Vector3Int[] extractionAxis = new Vector3Int[] {
        Vector3Int.right,
        Vector3Int.up,
        vector3IntForward,
        Vector3Int.right + Vector3Int.up + vector3IntForward,
        Vector3Int.right + Vector3Int.up,
        Vector3Int.up + vector3IntForward,
        Vector3Int.right + vector3IntForward,
    };
    private Voxel[] ExtractVoxels (uint nodeLocation, bool withEdges) {
        var depth = Octree.GetDepth (nodeLocation);

        if (depth == chunks.GetMaxDepth ()) { }

        var node = chunks.GetNode (nodeLocation);

        var voxelArraySize = 19;
        Voxel[] nodeVoxels = new Voxel[voxelArraySize * voxelArraySize * voxelArraySize];

        // for (int i = 0; i < nodeVoxels.Length; i++) nodeVoxels[i] = new Voxel { density = 1f };

        void FillSides (int axis) {

            var negativeNeighbour = chunks.GetNode (Octree.RelativeLeafNodeLocation (nodeLocation, -extractionAxis[axis]));
            negativeNeighbour.chunk.voxels.Traverse ((x, y, z, v) => {
                var edgeIndex = chunkSize - 1;
                if (axis == 0 && x == edgeIndex) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (0, y, z), voxelArraySize)] = v;
                }
                if (axis == 1 && y == edgeIndex) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (x, 0, z), voxelArraySize)] = v;
                }
                if (axis == 2 && z == edgeIndex) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (x, y, 0), voxelArraySize)] = v;
                }
                if (axis == 3 && x == edgeIndex && y == edgeIndex && z == edgeIndex) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (0, 0, 0), voxelArraySize)] = v;
                }
                if (axis == 4 && x == edgeIndex && y == edgeIndex) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (0, 0, z), voxelArraySize)] = v;
                }
                if (axis == 5 && z == edgeIndex && y == edgeIndex) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (x, 0, 0), voxelArraySize)] = v;
                }
                if (axis == 6 && x == edgeIndex && z == edgeIndex) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (0, y, 0), voxelArraySize)] = v;
                }
            });

            var positiveNeighbour = chunks.GetNode (Octree.RelativeLeafNodeLocation (nodeLocation, extractionAxis[axis]));
            positiveNeighbour.chunk.voxels.Traverse ((x, y, z, v) => {
                int edgeStartIndex = voxelArraySize - 2;
                if (axis == 0 && x <= 1) {
                    // Debug.Log ($"\tx:{x}\t\ty:{y}\t\tz:{z}\tx+edgeStart:{edgeStartIndex + x}");
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (edgeStartIndex + x, y + 1, z + 1), voxelArraySize)] = v;
                }
                if (axis == 1 && y <= 1) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (x + 1, edgeStartIndex + y, z + 1), voxelArraySize)] = v;
                }
                if (axis == 2 && z <= 1) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (x + 1, y + 1, edgeStartIndex + z), voxelArraySize)] = v;
                }
                if (axis == 3 && x <= 1 && y <= 1 && z <= 1) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (edgeStartIndex + x, edgeStartIndex + y, edgeStartIndex + z), voxelArraySize)] = v;
                }
                if (axis == 4 && x <= 1 && y <= 1) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (edgeStartIndex + x, edgeStartIndex + y, z + 1), voxelArraySize)] = v;
                }
                if (axis == 5 && z <= 1 && y <= 1) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (1 + x, edgeStartIndex + y, edgeStartIndex + z), voxelArraySize)] = v;
                }
                if (axis == 6 && x <= 1 && z <= 1) {
                    nodeVoxels[Util.Map3DTo1D (new Vector3Int (edgeStartIndex + x, 1 + y, edgeStartIndex + z), voxelArraySize)] = v;
                }
            });
        }

        for (int i = 0; i < extractionAxis.Length; i++) {
            FillSides (i);
        }

        node.chunk.voxels.Traverse ((x, y, z, v) => {
            nodeVoxels[Util.Map3DTo1D (new Vector3Int (x + 1, y + 1, z + 1), voxelArraySize)] = v;
        });

        return nodeVoxels;
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
        DrawNodeIfExists (Octree.RelativeLocation (from.locationCode, axis, direction));
    }

    void DrawNodeIfExists (uint location) {
        var positiveNeighbour = chunks.GetNode (location);
        if (positiveNeighbour != null)
            Gizmos.DrawWireCube (positiveNeighbour.bounds.center, positiveNeighbour.bounds.size);
    }

    Color idleColor = new Color (1, 1, 1, .05f);

    void OnDrawGizmos () {
        if (chunks == null) return;

        Gizmos.color = new Color (1, .3f, .2f, 1f);
        lod0Nodes.ToList ().ForEach (n => DrawNodeIfExists (n));
        Gizmos.color = new Color (.6f, 1f, .4f, .2f);
        lod1Nodes.ToList ().ForEach (n => DrawNodeIfExists (n));
        Gizmos.color = new Color (.7f, .7f, 1f, .1f);
        lod2Nodes.ToList ().ForEach (n => DrawNodeIfExists (n));

        chunks.GetLeafChildren (0b1).ForEach (node => {
            Gizmos.color = new Color (1, 0, 0, .05f);
            if (node.chunk.hasData)
                Gizmos.DrawWireCube (node.bounds.center, node.bounds.size);
        });

        // var currentNode = chunks.GetNode (chunks.GetNodeIndexAtCoord (viewerCoordinates));
        // if (currentNode != null) {

        //     void DrawParents (uint locationCode) {
        //         Gizmos.color = Color.green;
        //         DrawNodeIfExists (locationCode >> 3);
        //         Gizmos.color = Color.yellow;
        //         DrawNodeIfExists (locationCode >> 6);
        //         Gizmos.color = Color.red;
        //         DrawNodeIfExists (locationCode >> 9);
        //     }
        //     Gizmos.DrawWireCube (currentNode.bounds.center, currentNode.bounds.size);
        //     DrawParents (currentNode.locationCode);

        //     for (int i = 0; i < cornerOffsets.Length; i++) {
        //         Gizmos.color = Color.magenta;
        //         uint code = chunks.RelativeLeafNodeLocation (currentNode.locationCode, cornerOffsets[i]);
        //         DrawNodeIfExists (code);
        //         DrawParents (code);

        //     }

        // }

    }
}