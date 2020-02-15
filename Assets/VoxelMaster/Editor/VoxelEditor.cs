using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(VoxelWorld))]
public class VoxelEditor : Editor
{

    public WorldGeneratorSettings worldGeneratorSettings = new WorldGeneratorSettings { worldSize = 512 };
    private List<Type> dataStructures = Util.GetEnumerableOfType<VoxelDataStructure>().ToList();
    private int dataStructureIndex = 0;

    private List<Type> meshGenerators = Util.GetEnumerableOfType<VoxelMeshGenerator>().ToList();
    private int meshGeneratorIndex = 0;
    private List<Type> heightmapGenerators = Util.GetEnumerableOfType<HeightmapGenerator>().ToList();
    private int heightmapGeneratorIndex = 0;
    private bool expandHeightmapGenerator = true;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        VoxelWorld voxelWorld = (VoxelWorld)target;



        GUIContent dataStructureLabel = new GUIContent("Data structure");
        dataStructureIndex = EditorGUILayout.Popup(
            dataStructureLabel,
            voxelWorld.dataStructure == null ? 0 : dataStructures.IndexOf(voxelWorld.dataStructure.GetType()),
            dataStructures.Select(x => FormatClassName(x.Name)).ToArray()
        );
        if (voxelWorld.dataStructure == null || voxelWorld.dataStructure.GetType() != dataStructures.ElementAt(dataStructureIndex))
            voxelWorld.dataStructure = Activator.CreateInstance(dataStructures.ElementAt(dataStructureIndex)) as VoxelDataStructure;


        GUIContent meshGeneratorLabel = new GUIContent("Mesh generator");
        meshGeneratorIndex = EditorGUILayout.Popup(
            meshGeneratorLabel,
            voxelWorld.meshGenerator == null ? 0 : meshGenerators.IndexOf(voxelWorld.meshGenerator.GetType()),
            meshGenerators.Select(x => FormatClassName(x.Name)).ToArray()
        );
        voxelWorld.meshGenerator = Activator.CreateInstance(meshGenerators.ElementAt(meshGeneratorIndex)) as VoxelMeshGenerator;

        GUIContent heightmapGeneratorLabel = new GUIContent("Heightmap generator");
        heightmapGeneratorIndex = EditorGUILayout.Popup(
            heightmapGeneratorLabel,
            voxelWorld.heightmapGenerator == null ? 0 : heightmapGenerators.IndexOf(voxelWorld.heightmapGenerator.GetType()),
            heightmapGenerators.Select(x => FormatClassName(x.Name)).ToArray()
        );
        if (voxelWorld.heightmapGenerator == null || voxelWorld.heightmapGenerator.GetType() != heightmapGenerators.ElementAt(heightmapGeneratorIndex))
            voxelWorld.heightmapGenerator = Activator.CreateInstance(heightmapGenerators.ElementAt(heightmapGeneratorIndex), 1332347) as HeightmapGenerator;



        expandHeightmapGenerator = EditorGUILayout.BeginFoldoutHeaderGroup(expandHeightmapGenerator, "Heightmap Generator Settings");
        if (expandHeightmapGenerator)
        {
            var generator = voxelWorld.heightmapGenerator;
            GUILayout.Label(preview);
            generator.OnInspectorGUI();
            if (GUILayout.Button("Generate Preview"))
            {
                voxelWorld.sdfsddsfsdf = preview = generator.GeneratePreviewTexture(worldGeneratorSettings);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }

    }

    private Texture2D preview;

    public static string FormatClassName(string className)
    {
        var regex = new Regex(@"(?<!^)[A-Z]+");
        return regex.Replace(className, " $&");
    }

}