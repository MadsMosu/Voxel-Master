using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoxelMaster;

[CustomEditor(typeof(VoxelWorld))]
public class VoxelEditor : Editor {

    VoxelTool currentTool;
    List<VoxelTool> tools = new List<VoxelTool>();

    float toolRadius = 3f;
    float toolFalloff = 0;

    private void Awake() {
        foreach (var tool in Util.GetEnumerableOfType<VoxelTool>()) {
            tools.Add(Util.CreateInstance<VoxelTool>(tool.AssemblyQualifiedName));
        }
    }


    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        VoxelWorld voxelWorld = (VoxelWorld)target;


        EditorGUILayout.LabelField("Editor Tools");

        GUILayout.BeginHorizontal("Terrain Tools");
        foreach (var tool in tools) {
            GUI.backgroundColor = tool == currentTool ? Color.green : Color.gray;
            if (GUILayout.Button(tool.name, GUILayout.Height(64))) {
                currentTool = tool;
            }
        }
        GUILayout.EndHorizontal();

        GUI.backgroundColor = Color.gray;
        GUILayout.BeginVertical("Tool Settings");
        EditorGUILayout.LabelField("Tool Radius");
        toolRadius = GUILayout.HorizontalSlider(toolRadius, 0, 10);
        EditorGUILayout.LabelField("Tool Falloff");
        toolFalloff = GUILayout.HorizontalSlider(toolFalloff, 0, 1);
        GUILayout.EndVertical();


        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }

    }

    void OnSceneGUI() {
        VoxelWorld voxelWorld = (VoxelWorld)target;

        // Ray ray = Camera.current. (Event.current.mousePosition);
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Handles.Label(voxelWorld.transform.position, "LABEL TEXT");
        Handles.SphereHandleCap(3, voxelWorld.transform.position, Quaternion.identity, 1, EventType.Repaint);
        RaycastHit hit = new RaycastHit();
        Handles.DrawLine(ray.origin, ray.origin + ray.direction * 1000);
        if (Physics.Raycast(ray, out hit, 1000.0f)) {
            Handles.color = Color.red;
            Handles.SphereHandleCap(-3, hit.point, Quaternion.identity, 1, EventType.Repaint);
            //if(Input.)
            Event.current.Use();
        }

    }

}