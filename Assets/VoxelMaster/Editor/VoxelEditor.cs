using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor (typeof (VoxelWorld))]
public class VoxelEditor : Editor {

    private List<Type> dataStructures = Util.GetEnumerableOfType<VoxelDataStructure> ().ToList ();
    private int dataStructureIndex = 0;

    private List<Type> meshGenerators = Util.GetEnumerableOfType<VoxelMeshGenerator> ().ToList ();
    private int meshGeneratorIndex = 0;

    public override void OnInspectorGUI () {
        base.OnInspectorGUI ();

        VoxelWorld voxelWorld = (VoxelWorld) target;

        if (voxelWorld.dataStructureType != null && voxelWorld.dataStructureType != "")
            Mathf.Clamp (dataStructureIndex = dataStructures.IndexOf (Type.GetType (voxelWorld.dataStructureType)), 0, dataStructures.Count - 1);
        if (voxelWorld.meshGeneratorType != null && voxelWorld.meshGeneratorType != "")
            Mathf.Clamp (meshGeneratorIndex = meshGenerators.IndexOf (Type.GetType (voxelWorld.meshGeneratorType)), 0, meshGenerators.Count - 1);

        GUIContent dataStructureLabel = new GUIContent ("Data structure");
        dataStructureIndex = EditorGUILayout.Popup (dataStructureLabel, dataStructureIndex, dataStructures.Select (x => Util.FormatClassName (x.Name)).ToArray ());
        voxelWorld.dataStructureType = dataStructures.ElementAt (dataStructureIndex).AssemblyQualifiedName;

        GUIContent meshGeneratorLabel = new GUIContent ("Mesh generator");
        meshGeneratorIndex = EditorGUILayout.Popup (meshGeneratorLabel, meshGeneratorIndex, meshGenerators.Select (x => Util.FormatClassName (x.Name)).ToArray ());
        voxelWorld.meshGeneratorType = meshGenerators.ElementAt (meshGeneratorIndex).AssemblyQualifiedName;

        if (GUI.changed) {
            EditorUtility.SetDirty (target);
        }

    }

}