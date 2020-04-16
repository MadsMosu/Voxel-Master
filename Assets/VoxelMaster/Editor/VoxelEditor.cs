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

        EditorGUILayout.LabelField ("Editor Tools");
        if (GUILayout.Button ("Add/Remove Density")) {

        }

        if (GUI.changed) {
            EditorUtility.SetDirty (target);
        }

    }

    void OnSceneGUI () {
        VoxelWorld voxelWorld = (VoxelWorld) target;

        // Ray ray = Camera.current. (Event.current.mousePosition);
        Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
        Handles.Label (voxelWorld.transform.position, "LABEL TEXT");
        Handles.SphereHandleCap (3, voxelWorld.transform.position, Quaternion.identity, 1, EventType.Repaint);
        RaycastHit hit = new RaycastHit ();
        Handles.DrawLine (ray.origin, ray.origin + ray.direction * 1000);
        if (Physics.Raycast (ray, out hit, 1000.0f)) {
            Handles.color = Color.red;
            Handles.SphereHandleCap (-3, hit.point, Quaternion.identity, 1, EventType.Repaint);
            Event.current.Use ();
        }

    }

}