
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelMaster
{
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
            chunks.TryGetValue(coords, out chunk);
            return chunk;
        }

        public override void ForEach(Action<Chunk> func)
        {
            foreach (var chunk in chunks.Values)
            {
                func.Invoke(chunk);
            }
        }

        public override bool ChunkExists(Vector3Int coords)
        {
            return chunks.ContainsKey(coords);
        }
    }
}
