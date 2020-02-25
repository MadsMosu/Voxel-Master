using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WorldGenerator : EditorWindow
{

    public WorldGeneratorSettings generatorSettings { get; private set; } = new WorldGeneratorSettings { worldSize = 200 };

    private List<Type> heightmapGenerators = Util.GetEnumerableOfType<HeightmapGenerator>().ToList();
    private List<Type> featureGenerators = Util.GetEnumerableOfType<FeatureGenerator>().ToList();
    private int heightmapGeneratorIndex = 0;
    private bool expandHeightmapGenerator = true, expandFeatures = true;
    private HeightmapGenerator heightmapGenerator;
    private float[] heightmapPreview;


    private GameObject previewPlane;
    private Material terrainPreviewMaterial;

    [MenuItem("VoxelMaster/Generate new terrain")]
    static void Create()
    {
        EditorWindow.GetWindow(typeof(WorldGenerator));
    }


    void Awake()
    {
        terrainPreviewMaterial = (Material)Resources.Load("TerrainMaterial", typeof(Material));
        Debug.Log(terrainPreviewMaterial);
        if (previewPlane == null)
        {
            previewPlane = new GameObject("preview plane");
            previewPlane.AddComponent<MeshFilter>();
            var meshRenderer = previewPlane.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = terrainPreviewMaterial;
            // meshRenderer.sharedMaterial.mainTexture = heightmapGenerator.GeneratePreviewTexture(generatorSettings);
        }
    }

    void OnDestroy()
    {
        if (previewPlane != null)
            GameObject.DestroyImmediate(previewPlane);
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
            heightmapGenerator = Activator.CreateInstance(heightmapGenerators.ElementAt(heightmapGeneratorIndex)) as HeightmapGenerator;


        if (GUILayout.Button("Randomize seed(" + generatorSettings.seed + ")")) generatorSettings.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        expandHeightmapGenerator = EditorGUILayout.BeginFoldoutHeaderGroup(expandHeightmapGenerator, "Heightmap Generator Settings");
        if (expandHeightmapGenerator)
        {
            GUILayout.Label(previewTexture);
            heightmapGenerator.OnInspectorGUI();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        generatorSettings.heightAmplifier = EditorGUILayout.Slider("Height amplifier", generatorSettings.heightAmplifier, 1f, 10f);

        EditorGUILayout.Separator();
        expandFeatures = EditorGUILayout.BeginFoldoutHeaderGroup(expandFeatures, "Terrain filters");
        if (expandFeatures)
        {

            foreach (var feature in featureGenerators)
            {
                GUILayout.Label(feature.Name);
            }

        }
        EditorGUILayout.EndFoldoutHeaderGroup();


        EditorGUILayout.Separator();

        if (GUILayout.Button("Generate Voxel World"))
        {

        }

        if (GUI.changed)
        {
            previewTexture = heightmapGenerator.GeneratePreviewTexture(generatorSettings);
            GeneratePreview();
        }

    }

    private Texture2D previewTexture;

    void GeneratePreview()
    {
        var heights = heightmapGenerator.Generate(generatorSettings);
        previewPlane.GetComponent<MeshFilter>().mesh = Util.GeneratePreviewPlane(heights, generatorSettings);
    }
}