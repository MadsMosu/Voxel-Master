using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TerrainGeneratorEditor : EditorWindow
{

    static NodeGraph activeNodeGraph;
    static Node selectedNode;

    public static void ShowWindow(NodeGraph graph)
    {
        activeNodeGraph = graph;
        GetWindow<TerrainGeneratorEditor>("Terrain Editor");
    }

    void OnGUI()
    {
        GUILayout.Label("TEST");
        if (activeNodeGraph != null)
        {
            GUILayout.Label(activeNodeGraph.name);
            foreach (var node in activeNodeGraph.nodes)
            {
                Rect nodeRect = new Rect(node.position.x, node.position.y, 100, 100);

                GUI.backgroundColor = (node != selectedNode) ? Color.white : Color.blue;
                GUI.Box(nodeRect, node.nodeName);
                if (Event.current.type == EventType.MouseDown)
                {
                    if (nodeRect.Contains(Event.current.mousePosition))
                    {
                        selectedNode = node;
                    }
                }

            }
            if (Event.current.type == EventType.MouseDrag && selectedNode != null)
            {
                selectedNode.position = new Vector2(Event.current.mousePosition.x - 50, Event.current.mousePosition.y - 50);
                Event.current.Use();
            }
        }
    }

    [UnityEditor.Callbacks.OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        if (Selection.activeObject as NodeGraph != null)
        {
            ShowWindow(Selection.activeObject as NodeGraph);
            return true;
        }

        return false;
    }

}
