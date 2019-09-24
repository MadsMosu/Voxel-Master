
using System;
using System.Collections.Generic;
using UnityEngine;
public class ChunkDictionary : ChunkDataStructure
{
    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();


    public override void AddChunk(Vector3Int coords, Chunk chunk)
    {
        chunks.Add(coords, chunk);
    }

    public override Chunk GetChunk(Vector3Int coords)
    {
        Chunk chunk;
        return chunks.TryGetValue(coords, out chunk) ? chunk : null;  
    }

    public override void ForEach(Action<Chunk> func)
    {
        foreach (var chunk in chunks.Values)
        {
            func.Invoke(chunk);
        }
    }

}