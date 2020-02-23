

using System.Runtime.InteropServices;
using System.Security;
using UnityEditor;
using UnityEngine;

class FlatTerrainGenerator : HeightmapGenerator
{

    FastNoise noise;

    public FlatTerrainGenerator(int seed)
    {
        noise = new FastNoise(seed);
    }

    public override float[] Generate(WorldGeneratorSettings settings)
    {
        var worldSize = settings.worldSize;
        var heightmap = new float[worldSize * worldSize];

        for (int x = 0; x < worldSize; x++)
            for (int y = 0; y < worldSize; y++)
            {
                var coord = new Vector2Int(x, y);
                heightmap[Util.Map2DTo1D(coord, Vector2Int.one * worldSize)] = 0.5f;
            }
        return heightmap;
    }

    public override Texture2D GeneratePreviewTexture(WorldGeneratorSettings settings)
    {
        throw new System.NotImplementedException();
    }

    public override void OnInspectorGUI()
    {


    }
}