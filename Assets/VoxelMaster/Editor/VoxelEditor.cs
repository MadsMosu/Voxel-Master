using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoxelMaster;
using VoxelMaster.Chunk;

[CustomEditor (typeof (VoxelWorld))]
public class VoxelEditor : Editor {

    VoxelTool currentTool;
    List<VoxelTool> tools = new List<VoxelTool> ();

    float toolIntensity = 0.1f;
    float toolRadius = 3f;
    float toolFalloff = 0;
    int controlId;

    private void Awake () {
        foreach (var tool in Util.GetEnumerableOfType<VoxelTool> ()) {
            tools.Add (Util.CreateInstance<VoxelTool> (tool.AssemblyQualifiedName));
        }
    }

    private void OnEnable () {
        controlId = GUIUtility.GetControlID (FocusType.Passive);
    }

    public override void OnInspectorGUI () {
        base.OnInspectorGUI ();

        VoxelWorld voxelWorld = (VoxelWorld) target;

        EditorGUILayout.LabelField ("Editor Tools");

        GUILayout.BeginHorizontal ("Terrain Tools");
        foreach (var tool in tools) {
            GUI.backgroundColor = tool == currentTool ? Color.green : Color.gray;
            if (GUILayout.Button (tool.name, GUILayout.Height (64))) {
                if (currentTool == tool) {
                    currentTool = null;
                } else currentTool = tool;
            }
        }
        GUILayout.EndHorizontal ();

        GUI.backgroundColor = Color.gray;
        GUILayout.BeginVertical ("Tool Settings");
        EditorGUILayout.LabelField ("Tool Intensity");
        toolIntensity = GUILayout.HorizontalSlider (toolIntensity, 0, 1);
        EditorGUILayout.LabelField ("Tool Radius");
        toolRadius = GUILayout.HorizontalSlider (toolRadius, 0, 10);
        EditorGUILayout.LabelField ("Tool Falloff");
        toolFalloff = GUILayout.HorizontalSlider (toolFalloff, 0, 1);
        GUILayout.EndVertical ();

        if (currentTool != null) {
            currentTool.OnToolGUI ();
        }

        if (GUI.changed) {
            EditorUtility.SetDirty (target);
        }

    }

    void OnSceneGUI () {
        VoxelWorld voxelWorld = (VoxelWorld) target;

        // Ray ray = Camera.current. (Event.current.mousePosition);
        if (currentTool != null) {
            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            Handles.Label (voxelWorld.transform.position, "LABEL TEXT");
            Handles.SphereHandleCap (3, voxelWorld.transform.position, Quaternion.identity, 1, EventType.Repaint);
            RaycastHit hit = new RaycastHit ();
            Handles.DrawLine (ray.origin, ray.origin + ray.direction * 1000);
            if (Physics.Raycast (ray, out hit, 1000.0f)) {
                Handles.color = Color.red;
                Handles.SphereHandleCap (-3, hit.point, Quaternion.identity, 1, EventType.Repaint);

                int resolution = 30;
                float rayDistance = 3;
                float currentAngle = 0;
                float radius = 3;
                Vector3[] circlePoints = new Vector3[resolution];
                for (int i = 0; i < resolution; i++) {
                    float x = Mathf.Sin (currentAngle);
                    float y = Mathf.Cos (currentAngle);
                    currentAngle += 2 * Mathf.PI / resolution;

                    Vector3 dir = -hit.normal;
                    RaycastHit hitCircle;
                    Debug.DrawLine (hit.point, dir, Color.red);
                    if (Physics.Raycast (hit.point + (new Vector3 (x, 0, y) * radius) + hit.normal * rayDistance, dir, out hitCircle)) {
                        circlePoints[i] = hitCircle.point;
                    }
                }

                for (int i = 0; i < resolution - 1; i++) {
                    Handles.DrawLine (circlePoints[i], circlePoints[i + 1]);
                }
                HandleUtility.AddDefaultControl (GUIUtility.GetControlID (FocusType.Passive));
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0) {
                    List<VoxelChunk> affectedChunks = GetAffectedChunks ((VoxelWorld) target, voxelWorld, hit.point);
                    foreach (var chunk in affectedChunks) {
                        currentTool.ToolDrag (voxelWorld, chunk, hit.point, Vector3.zero, toolIntensity, Mathf.CeilToInt (toolRadius));
                    }
                }
            }
        }
    }

    private List<VoxelChunk> GetAffectedChunks (VoxelWorld target, IVoxelData volume, Vector3 center) {
        int ceiledRadius = Mathf.CeilToInt (toolRadius);
        //calculate sphere volume
        // int size = Mathf.CeilToInt (4 / 3 * Mathf.PI * Mathf.Pow (ceiledRadius, 3));
        List<VoxelChunk> affectedChunks = new List<VoxelChunk> ();
        var chunkCoord = new Vector3Int (
            Util.Int_floor_division ((int) center.x, (target.chunkSize - 1)),
            Util.Int_floor_division ((int) center.y, (target.chunkSize - 1)),
            Util.Int_floor_division ((int) center.z, (target.chunkSize - 1))
        );
        int temp = Mathf.CeilToInt (toolRadius / target.chunkSize);

        for (int x = chunkCoord.x - temp; x < chunkCoord.x + temp; x++)
            for (int y = chunkCoord.y - temp; y < chunkCoord.y + temp; y++)
                for (int z = chunkCoord.z - temp; z < chunkCoord.z + temp; z++) {
                    Vector3Int coords = new Vector3Int (x, y, z);
                    affectedChunks.Add (target.chunkDictionary[coords]);
                }
        return affectedChunks;
    }
}