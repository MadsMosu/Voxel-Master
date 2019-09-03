using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MarchingCubes;
using System.Linq;

public class VoxelGrid : MonoBehaviour
{

    public Transform target;
    private Vector3Int targetCoords;
    public int searchRadius = 5;

    public Material material;

    public int chunkSize = 16;
    public float voxelSize = 2f;

    [SerializeField]
    private LODLevel[] lodLevels;

    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    List<Chunk> visibleChunks = new List<Chunk>();

    WorldGenerator worldGenerator = new WorldGenerator(new WorldSettings());
    MeshGenerator meshGenerator = new MeshGenerator(new MeshSettings());


    private void Start()
    {
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

        for (int i = 0; i < visibleChunks.Count; i++)
        {
            var chunk = visibleChunks[i];
            if (Vector3Int.Distance(chunk.coords, targetCoords) > searchRadius * chunkSize)
                chunk.Visibility = false;
        }

        UpdateChunk(targetCoords);

        Vector3Int[] chunksToBeUpdated = new Vector3Int[(int)Mathf.Pow(searchRadius * 2 + 1, 3)];

        int index = 0;
        for (int x = -searchRadius; x <= searchRadius; x++)
            for (int y = -searchRadius; y <= searchRadius; y++)
                for (int z = -searchRadius; z <= searchRadius; z++)
                {
                    var coords = targetCoords + new Vector3Int(x, y, z);
                    chunksToBeUpdated[index++] = coords;
                }

        foreach (var chunk in chunksToBeUpdated.OrderBy(a => Vector3Int.Distance(a, targetCoords)))
        {
            UpdateChunk(chunk);
        }

        foreach (var chunk in visibleChunks)
        {
            chunk.Visibility = (Vector3Int.Distance(chunk.coords, targetCoords) < searchRadius + searchRadius * 10);
        }
    }

    void UpdateChunk(Vector3Int coords)
    {
        if (!chunks.ContainsKey(coords))
        {
            var c = new Chunk(coords, chunkSize, voxelSize, worldGenerator, meshGenerator, material, lodLevels, target);
            chunks.Add(coords, c);
            visibleChunks.Add(c);
            c.Load();
        }
    }

}

[System.Serializable]
public struct LODLevel
{
    public int lod;
    public float distance;
}