using UnityEngine;


public class VoxelGrid : MonoBehaviour
{
    private ChunkDataStructure chunks = new ChunkDictionary();
    public Transform viewer;
    public int ChunkSize = 16;

    public TerrainGraph terrainGraph = new TerrainGraph();

    void Update()
    {
        GenerateChunks();
    }

    void GenerateChunks()
    {
        Vector3Int viewerCoordinate = new Vector3Int(
            Mathf.FloorToInt(viewer.position.x / ChunkSize), 
            Mathf.FloorToInt(viewer.position.y / ChunkSize), 
            Mathf.FloorToInt(viewer.position.z / ChunkSize)
        );
        if (chunks.GetChunk(viewerCoordinate) == null)
        {
            chunks.AddChunk(viewerCoordinate, new Chunk(viewerCoordinate, ChunkSize));
        }
    }

    void OnDrawGizmos()
    {
        chunks.ForEach(c =>
        {
            Gizmos.DrawWireCube((c.coords * ChunkSize) + Vector3.one * (ChunkSize / 2), ChunkSize * Vector3.one);
        });
    }




}