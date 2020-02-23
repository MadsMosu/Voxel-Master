

using System.Runtime.InteropServices;
using System.Security;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

[Serializable]
class FractalHeightmapGenerator : HeightmapGenerator
{

    FastNoise noise;

    float fractalNoiseScale = 1f;

    public FractalHeightmapGenerator(int seed)
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
                var xs = x * fractalNoiseScale;
                var ys = y * fractalNoiseScale;

                var height = noise.GetPerlinFractal(xs, ys);

                // height -= noise.GetCellular(xs, ys) * noise.GetCubicFractal(xs / 10.0f, ys / 10.0f);

                var coord = new Vector2Int(x, y);
                heightmap[Util.Map2DTo1D(coord, Vector2Int.one * worldSize)] = height;
            }
        return heightmap;
    }


    public override Texture2D GeneratePreviewTexture(WorldGeneratorSettings settings)
    {
        var worldSize = settings.worldSize;
        var heightmapData = Generate(settings);
        Texture2D result = new Texture2D(settings.worldSize, settings.worldSize);
        var colors = heightmapData.Select(h => Color.Lerp(Color.black, Color.white, h)).ToArray();
        result.SetPixels(0, 0, worldSize, worldSize, colors);
        result.Apply();
        return result;
    }


    public override void OnInspectorGUI()
    {
        fractalNoiseScale = EditorGUILayout.Slider(fractalNoiseScale, .1f, 20f);
    }


}