using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelWorld))]
public class VoxelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        VoxelWorld voxelWorld = (VoxelWorld)target;

        GUIContent dataStructureLabel = new GUIContent("Data structure");
        voxelWorld.dataStructureIndex = EditorGUILayout.Popup(
            dataStructureLabel,
            voxelWorld.dataStructureIndex,
            voxelWorld.dataStructures.Select(x => x.GetType().Name).ToArray()
        );

        GUIContent meshGeneratorLabel = new GUIContent("Mesh generator");
        voxelWorld.meshGeneratorIndex = EditorGUILayout.Popup(
            meshGeneratorLabel,
            voxelWorld.meshGeneratorIndex,
            voxelWorld.meshGenerators.Select(x => x.GetType().Name).ToArray()
        );
    }
}