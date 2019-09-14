using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MarchingCubes;
using System.Linq;

public class VoxelGrid : MonoBehaviour
{
    public TerrainGraph terrainGraph;
    public Transform target;
    private Vector3Int targetCoords;
    public int searchRadius = 5;

    public Material material;

    public int chunkSize = 16;
    public float voxelSize = 2f;

    [SerializeField]
    private LODLevel[] lodLevels;

    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    public List<Chunk> visibleChunks = new List<Chunk>();

    WorldGenerator worldGenerator;
    MeshGenerator meshGenerator = new MeshGenerator(new MeshSettings());


    private void Start()
    {
        worldGenerator = new WorldGenerator(new WorldSettings(), terrainGraph);
    }

    private void Update()
    {
        UpdateChunks();

        worldGenerator.MainThreadUpdate();
        meshGenerator.MainThreadUpdate();

    }

    void UpdateChunks()
    {
        targetCoords = new Vector3Int(
            Mathf.RoundToInt(target.position.x / chunkSize),
            Mathf.RoundToInt(target.position.y / chunkSize),
            Mathf.RoundToInt(target.position.z / chunkSize)
        );

        UpdateChunk(targetCoords);
        UpdateFirstUnloadedChunkInSearchDistance();


        foreach (var chunk in visibleChunks)
        {
            chunk.Visibility = (Vector3Int.Distance(chunk.coords, targetCoords) < searchRadius + searchRadius * 10);
        }
    }


    void UpdateFirstUnloadedChunkInSearchDistance()
    {
        for (int x = 0; x <= searchRadius; x++)
            for (int y = 0; y <= searchRadius; y++)
                for (int z = 0; z <= searchRadius; z++)
                {
                    var positive = targetCoords + new Vector3Int(x, y, z);
                    var negative = targetCoords - new Vector3Int(x, y, z);
                    if (!chunks.ContainsKey(positive) || !chunks.ContainsKey(negative))
                    {
                        UpdateChunk(positive);
                        UpdateChunk(negative);
                        return;
                    }
                }
    }

    void UpdateChunk(Vector3Int coords)
    {
        if (!chunks.ContainsKey(coords))
        {
            var c = new Chunk(coords, chunkSize, voxelSize, worldGenerator, meshGenerator, material, lodLevels, target);
            c.GenerateLODMeshes();
            chunks.Add(coords, c);
            visibleChunks.Add(c);
            c.Load();
        }
    }

    Chunk GetChunkAddPosition(Vector3 position)
    {
        var x = Mathf.FloorToInt(position.x / (chunkSize * voxelSize));
        var y = Mathf.FloorToInt(position.y / (chunkSize * voxelSize));
        var z = Mathf.FloorToInt(position.z / (chunkSize * voxelSize));
        return chunks[new Vector3Int(x, y, z)];
    }

    public void addDensity(Vector3 origin, float amount)
    {
        var chunk = GetChunkAddPosition(origin);
        var chunkSpaceOrigin = new Vector3Int(
            Mathf.FloorToInt(origin.x / chunk.size * voxelSize),
            Mathf.FloorToInt(origin.y / chunk.size * voxelSize),
            Mathf.FloorToInt(origin.y / chunk.size * voxelSize)
        );
        chunk.addDensity(chunkSpaceOrigin, amount);
    }

    public void reduceDensity(Vector3 origin)
    {

    }
}

[System.Serializable]
public struct LODLevel
{
    public int lod;
    public float distance;
}