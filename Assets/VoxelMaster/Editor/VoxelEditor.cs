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
        voxelWorld.dataStructure = EditorGUILayout.Popup(dataStructureLabel, voxelWorld.dataStructure, voxelWorld.dataStructures.Select(x => x.GetType().Name).ToArray());
    }
}