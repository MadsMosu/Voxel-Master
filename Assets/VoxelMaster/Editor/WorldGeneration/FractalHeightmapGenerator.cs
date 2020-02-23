

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

    float fractalNoiseScale = 1.001f;
    bool islandShape = false;

    public FractalHeightmapGenerator(int seed)
    {
        noise = new FastNoise(seed);
    }

    public override float[] Generate(WorldGeneratorSettings settings)
    {
        var worldSize = settings.worldSize;
        var heightmap = new float[worldSize * worldSize];

        var worldCenter = new Vector2(worldSize / 2f, worldSize / 2f);

        for (int i = 0; i < heightmap.Length; i++)
        {
            var coord = Util.Map1DTo2D(i, worldSize);
            var x = coord.x;
            var y = coord.y;

            var xs = (x - worldSize / 2) * fractalNoiseScale;
            var ys = (y - worldSize / 2) * fractalNoiseScale;

            var height = noise.GetCubicFractal(xs, ys) + .5f;


            if (islandShape)
                height *= Mathf.Clamp01((worldSize / 2f - Vector2.Distance(new Vector2(x, y), worldCenter)) / (worldSize / 2));

            heightmap[i] = height;
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


    public override bool OnInspectorGUI()
    {
        var changed = false;

        var _fractalNoiseScale = EditorGUILayout.Slider(fractalNoiseScale, 0, 100f);
        if (fractalNoiseScale != _fractalNoiseScale)
        {
            fractalNoiseScale = _fractalNoiseScale;
            changed = true;
        }

        var _fadeOutEdges = EditorGUILayout.Toggle("Force island shape ", islandShape);
        if (islandShape != _fadeOutEdges)
        {
            islandShape = _fadeOutEdges;
            changed = true;
        }

        return changed;
    }


}