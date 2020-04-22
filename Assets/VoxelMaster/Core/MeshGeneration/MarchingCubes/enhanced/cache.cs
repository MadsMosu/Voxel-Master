using UnityEngine;

public class Cache {
    private int chunkSize;
    private Vector3[] regularCellVertexCache, transitionCellVertexCache;

    public Cache (int chunkSize) {
        this.chunkSize = chunkSize;
        regularCellVertexCache = new Vector3[chunkSize * chunkSize * chunkSize * 4];
        transitionCellVertexCache = new Vector3[6 * chunkSize * chunkSize * 10];
    }

    public Vector3 GetRegularCellVertex (Vector3Int cellPos, int reusableIndex) {
        return regularCellVertexCache[GetRegularCacheIndex (cellPos, reusableIndex)];
    }

    public void SetRegularCellVertex (int vertexIndex, Vector3Int cellPos, int reusableIndex) {
        var cacheIndex = GetRegularCacheIndex (cellPos, reusableIndex);
        regularCellVertexCache[cacheIndex] = vertexIndex;
    }

    public Vector3 GetTransitionCellVertex (int side, int u, int v, int reusableIndex) {
        return transitionCellVertexCache[GetTransitionCacheIndex (side, u, v, reusableIndex)];
    }

    public void SetTransitionCellVertex (int vertexIndex, int side, int u, int v, int reusableIndex) {
        var cacheIndex = GetTransitionCacheIndex (side, u, v, reusableIndex);
        transitionCellVertexCache[cacheIndex] = vertexIndex;
    }

    private int GetRegularCacheIndex (Vector3Int cellPos, int reusableIndex) {
        var cacheIndex = cellPos.x + chunkSize * cellPos.y + chunkSize * chunkSize * cellPos.z + chunkSize * chunkSize * chunkSize * reusableIndex;
        return cacheIndex;
    }

    private int GetTransitionCacheIndex (int side, int u, int v, int reusableIndex) {
        var cacheIndex = side + 6 * u + 6 * chunkSize * v + 6 * chunkSize * chunkSize * reusableIndex;
        return cacheIndex;
    }

    // private float GetRegularCellDensity (Vector3Int coords) {
    //     return 0f;
    // }

    // private float GetTransitionDensity (Vector3Int cellPos, int side, int u, int v, int w) {
    //     var cellOriginU = 2 * (Tables.transReverseOrientation[side][0].x * (chunkSize - 1) + cellPos.x * Tables.transReverseOrientation[side][1].x + cellPos.y * Tables.transReverseOrientation[side][2].x + cellPos.z * Tables.transReverseOrientation[side][3].x);
    //     var cellOriginV = 2 * (Tables.transReverseOrientation[side][0].y * (chunkSize - 1) + cellPos.x * Tables.transReverseOrientation[side][1].y + cellPos.y * Tables.transReverseOrientation[side][2].y + cellPos.z * Tables.transReverseOrientation[side][3].y);
    //     var cellOriginW = 0;
    //     var shiftedU = cellOriginU + u + 1;
    //     var shiftedV = cellOriginV + v + 1;
    //     var shiftedW = cellOriginW + w + 1;
    // }

}