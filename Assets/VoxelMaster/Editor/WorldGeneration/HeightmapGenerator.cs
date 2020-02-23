using System;


[Serializable]
public abstract class HeightmapGenerator
{
    public abstract float[] Generate(WorldGeneratorSettings settings);

    public abstract UnityEngine.Texture2D GeneratePreviewTexture(WorldGeneratorSettings settings);
    public abstract bool OnInspectorGUI();
}