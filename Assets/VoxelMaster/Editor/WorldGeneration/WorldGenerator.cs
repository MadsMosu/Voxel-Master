using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WorldGenerator : EditorWindow
{

    public WorldGeneratorSettings generatorSettings { get; private set; } = new WorldGeneratorSettings { worldSize = 64 };

    private List<Type> heightmapGenerators = Util.GetEnumerableOfType<HeightmapGenerator>().ToList();
    private int heightmapGeneratorIndex = 0;
    private bool expandHeightmapGenerator = true;
    private HeightmapGenerator heightmapGenerator;
    private float[] heightmapPreview;
    private float terrainAmplifier = 1;


    private GameObject previewPlane;
    private Material terrainPreviewMaterial = (Material)Resources.Load("TerrainMaterial", typeof(Material));

    [MenuItem("VoxelMaster/Generate new terrain")]
    static void Create()
    {
        EditorWindow.GetWindow(typeof(WorldGenerator));
    }


    void Awake()
    {
        if (previewPlane == null)
        {
            previewPlane = new GameObject("preview plane");
            previewPlane.AddComponent<MeshFilter>();
            var meshRenderer = previewPlane.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial.mainTexture = heightmapGenerator.GeneratePreviewTexture(generatorSettings);
        }
    }

    void OnDestroy()
    {
        if (previewPlane != null)
            GameObject.Destroy(previewPlane);
    }

    void OnGUI()
    {
        GUIContent heightmapGeneratorLabel = new GUIContent("Heightmap generator");
        heightmapGeneratorIndex = EditorGUILayout.Popup(
            heightmapGeneratorLabel,
            heightmapGenerator == null ? 0 : heightmapGenerators.IndexOf(heightmapGenerator.GetType()),
            heightmapGenerators.Select(x => Util.FormatClassName(x.Name)).ToArray()
        );
        if (heightmapGenerator == null || heightmapGenerator.GetType() != heightmapGenerators.ElementAt(heightmapGeneratorIndex))
            heightmapGenerator = Activator.CreateInstance(heightmapGenerators.ElementAt(heightmapGeneratorIndex), 1332347) as HeightmapGenerator;



        expandHeightmapGenerator = EditorGUILayout.BeginFoldoutHeaderGroup(expandHeightmapGenerator, "Heightmap Generator Settings");
        if (expandHeightmapGenerator)
        {
            GUILayout.Label(previewTexture);
            if (heightmapGenerator.OnInspectorGUI())
                GeneratePreview();

        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private Texture2D previewTexture;

    void GeneratePreview()
    {
        var heights = heightmapGenerator.Generate(generatorSettings);
        previewPlane.GetComponent<MeshFilter>().mesh = Util.GeneratePreviewPlane(heights, generatorSettings);
    }
}