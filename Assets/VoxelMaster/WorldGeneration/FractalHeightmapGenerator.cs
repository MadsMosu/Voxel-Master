using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
class FractalHeightmapGenerator : HeightmapGenerator {

    float fractalNoiseScale = 1.001f;
    float baseAmplitude = 1f;
    float detailAmplitude = 1f;
    bool islandShape = false;

    public override float[] Generate (WorldGeneratorSettings settings) {
        var noise = new FastNoise (settings.seed);

        var worldSize = settings.worldSize;
        var heightmap = new float[worldSize * worldSize];

        var worldCenter = new Vector2 (worldSize / 2f, worldSize / 2f);

        for (int i = 0; i < heightmap.Length; i++) {
            var coord = Util.Map1DTo2D (i, worldSize);
            var x = coord.x;
            var y = coord.y;

            var xs = (x - worldSize / 2) * fractalNoiseScale;
            var ys = (y - worldSize / 2) * fractalNoiseScale;

            var height = (noise.GetCubicFractal (xs, ys) + .5f) * baseAmplitude;
            height += (noise.GetPerlinFractal (xs * 2f, ys * 2f)) * detailAmplitude;

            height *= settings.heightAmplifier;

            if (islandShape)
                height *= Mathf.Clamp01 ((worldSize / 2f - Vector2.Distance (new Vector2 (x, y), worldCenter)) / (worldSize / 2));

            heightmap[i] = height;
        }

        return heightmap;
    }

    public override Texture2D GeneratePreviewTexture (WorldGeneratorSettings settings) {
        var worldSize = settings.worldSize;
        var heightmapData = Generate (settings);
        Texture2D result = new Texture2D (settings.worldSize, settings.worldSize);
        var colors = heightmapData.Select (h => Color.Lerp (Color.black, Color.white, h / settings.heightAmplifier)).ToArray ();
        result.SetPixels (0, 0, worldSize, worldSize, colors);
        result.Apply ();
        return result;
    }

    public override void OnInspectorGUI () {
        fractalNoiseScale = EditorGUILayout.Slider ("Fractal noise scale", fractalNoiseScale, 0, 25);
        baseAmplitude = EditorGUILayout.Slider ("Base amplitude", baseAmplitude, 0, 1);
        detailAmplitude = EditorGUILayout.Slider ("Detail amplitude", detailAmplitude, 0, 1);
        islandShape = EditorGUILayout.Toggle ("Force island shape ", islandShape);
    }

}