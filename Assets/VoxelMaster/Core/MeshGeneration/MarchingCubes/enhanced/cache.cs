using UnityEngine;

public struct ReuseCell {
    public readonly int[] vertexIndices;
    public ReuseCell (int size) {
        vertexIndices = new int[size];
        for (int i = 0; i < size; i++) {
            vertexIndices[i] = -1;
        }
    }
}

public sealed class RegularCellCache {
    private static RegularCellCache cacheInstance = null;
    private readonly ReuseCell[][] cache;
    private readonly int chunkSize;

    private RegularCellCache (int chunkSize) {
        this.chunkSize = chunkSize + 3;
        int cacheSize = this.chunkSize * this.chunkSize;
        cache = new ReuseCell[2][];
        cache[0] = new ReuseCell[cacheSize];
        cache[1] = new ReuseCell[cacheSize];

        for (int i = 0; i < cacheSize; i++) {
            cache[0][i] = new ReuseCell (4);
            cache[1][i] = new ReuseCell (4);
        }
    }
    public static RegularCellCache Cache (int chunkSize) {
        if (cacheInstance == null) {
            cacheInstance = new RegularCellCache (chunkSize);
        }
        return cacheInstance;
    }

    public ReuseCell GetReusedIndex (Vector3Int pos, byte rDir) {
        int rx = rDir & 1;
        int ry = (rDir >> 2) & 1;
        int rz = (rDir >> 1) & 1;

        int dx = pos.x - rx;
        int dy = pos.y - ry;
        int dz = pos.z - rz;

        return cache[dx & 1][dy * chunkSize + dz];
    }

    public void SetReusableIndex (Vector3Int pos, byte reuseIndex, ushort previousVertexIndex) {
        cache[pos.x & 1][pos.y * chunkSize + pos.z].vertexIndices[reuseIndex] = previousVertexIndex;
    }

    public ReuseCell this [int x, int y, int z] {
        set { cache[x & 1][y * chunkSize + z] = value; }
    }

    public ReuseCell this [Vector3Int v] {
        set { this [v.x, v.y, v.z] = value; }
    }
}