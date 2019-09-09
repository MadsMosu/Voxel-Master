using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class TerrainGeneratorEditor : EditorWindow
{

    static NodeGraph activeNodeGraph;
    static Node selectedNode;
    static Rect selectedPort;
    static Vector2 pan = new Vector2(0, 0);

    static Rect screen = new Rect(pan.x, pan.y, 100000, 100000);
    static Vector2 headerOffset = new Vector2(0, 30);
    Vector2 portSize = new Vector2(18, 18);
    static float sideOffset = 15;
    // static float portAspect = NodeStyle.portAspect;




    public static void ShowWindow(NodeGraph graph)
    {
        activeNodeGraph = graph;
        GetWindow<TerrainGeneratorEditor>("Terrain Editor");
    }

    void OnGUI()
    {
        if (activeNodeGraph != null)
        {
            NodeStyle.Init();

            GUI.BeginGroup(screen);

            HandleInput();
            DrawNodes();
            DrawPortKnobs();
            DrawPortConnections();

            GUI.EndGroup();
        }
    }


    void DrawGrid(Rect rect)
    {
        Vector2 center = rect.size / 2f;

        float xOffset = -(center.x / NodeStyle.background.width);
        float yOffset = ((center.y - rect.size.y) / NodeStyle.background.height);

        Vector2 tileOffset = new Vector2(xOffset, yOffset);

        float tileAmountX = Mathf.Round(rect.width / NodeStyle.background.width);
        float tileAmountY = Mathf.Round(rect.height / NodeStyle.background.height);

        Vector2 tileAmount = new Vector2(tileAmountX, tileAmountY);

        GUI.DrawTextureWithTexCoords(rect, NodeStyle.background, new Rect(tileOffset, tileAmount));
    }



    void HandleInput()
    {
        if (Event.current.type == EventType.MouseDown)
        {
            OnNodeClick(null);
            //TODO: deselect port.. its a rect, so it is non-nullable
            foreach (var node in activeNodeGraph.nodes)
            {
                if (new Rect(node.rect.x, node.rect.y, node.rect.width, headerOffset.y).Contains(Event.current.mousePosition))
                {
                    OnNodeClick(node);
                    break;
                }
                else
                {
                    foreach (var inputPort in node.inputs)
                    {
                        if (inputPort.port.Contains(Event.current.mousePosition))
                        {
                            OnPortClick(inputPort.port);
                        }
                    }
                    foreach (var outputPort in node.outputs)
                    {
                        if (outputPort.port.Contains(Event.current.mousePosition))
                        {
                            OnPortClick(outputPort.port);
                        }
                    }
                }
            }
        }
        if (Event.current.type == EventType.MouseDrag)
        {
            OnDrag();
        }
        if (Event.current.type == EventType.ContextClick)
        {
            var menu = new GenericMenu();
            var mousePos = Event.current.mousePosition;
            menu.AddItem(new GUIContent("Add addition node"), false, () => activeNodeGraph.AddNode<AddNode>(mousePos));
            menu.ShowAsContext();
        }
        if (Event.current.type == EventType.Repaint)
        {
            DrawGrid(screen);
        }
    }


    void DrawNodes()
    {
        foreach (var node in activeNodeGraph.nodes)
        {
            Rect nodeRect = node.rect;
            Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, headerOffset.y);
            GUI.Box(headerRect, GUIContent.none, NodeStyle.nodeBoxBold);
            GUI.Label(headerRect, node.nodeName, NodeStyle.nodeLabelBoldCentered);
            Rect bodyRect = new Rect(nodeRect.x, nodeRect.y + headerOffset.y, nodeRect.width, nodeRect.height - headerOffset.y);
            GUI.Box(bodyRect, GUIContent.none, GUI.skin.box);

            GUILayout.BeginArea(bodyRect, NodeStyle.bodyStyle);

            DrawPorts(node, bodyRect);

            GUILayout.EndArea();
        }
    }

    void DrawPorts(Node node, Rect bodyRect)
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();

        for (var i = 0; i < node.inputs.Length; i++)
        {
            var inputPort = node.inputs[i];
            var inputContent = new GUIContent(inputPort.name);

            GUILayout.Label(inputContent, node.inputs[i].connection.node != null ? NodeStyle.nodeLabelInputBold : NodeStyle.nodeLabelInput);
        }

        GUILayout.EndVertical();
        GUILayout.BeginVertical();

        for (var i = 0; i < node.outputs.Length; i++)
        {
            var outputPort = node.outputs[i];
            var outputContent = new GUIContent(outputPort.name);
            GUILayout.Label(outputContent, node.outputs[i].value != null ? NodeStyle.nodeLabelOutputBold : NodeStyle.nodeLabelOutput);
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    void DrawPortKnobs()
    {
        foreach (Node node in activeNodeGraph.nodes)
        {
            for (var i = 0; i < node.inputs.Length; i++)
            {
                if (node.inputs[i].connection.node != null)
                {
                    GUI.color = NodeStyle.wTextColor;
                }
                else GUI.color = NodeStyle.gTextColor;

                float portX = node.rect.x;
                float portY = node.rect.y;
                float knobX = portX - portSize.x / 2;
                float knobY = portY + headerOffset.y + sideOffset + ((sideOffset * (i)) * 2);
                Rect knobRect = new Rect(knobX, knobY, portSize.x, portSize.y);
                node.inputs[i].port = knobRect;
                GUI.DrawTexture(knobRect, NodeStyle.dot);
            }
            for (var i = 0; i < node.outputs.Length; i++)
            {
                if (node.outputs[i].value != null)
                {
                    GUI.color = NodeStyle.wTextColor;
                }
                else GUI.color = NodeStyle.gTextColor;

                float portX = node.rect.x + node.rect.width - portSize.x;
                float portY = node.rect.y;
                float knobX = portX + portSize.x / 2;
                float knobY = portY + headerOffset.y + sideOffset + ((sideOffset * (i)) * 2);
                Rect knobRect = new Rect(knobX, knobY, portSize.x, portSize.y);
                node.outputs[i].port = knobRect;
                GUI.DrawTexture(knobRect, NodeStyle.dot);


            }
        }
    }

    void DrawPortConnections()
    {
        foreach (var node in activeNodeGraph.nodes)
        {
            foreach (var inputPort in node.inputs)
            {
                if (inputPort.connection.node != null)
                {
                    var con = inputPort.connection;
                    DrawBezier(con.node.outputs[con.outputIndex].port.center, inputPort.port.center, NodeStyle.wTextColor);
                }
            }
        }
    }

    void DrawBezier(Vector2 start, Vector2 end, Color color)
    {
        Vector3 startPos = new Vector3(start.x, start.y, 0);
        Vector3 endPos = new Vector3(end.x, end.y, 0);
        // Vector3 startTan = startPos + Vector3.right * 50;
        // Vector3 endTan = endPos + Vector3.left * 50;
        float tangent = Mathf.Clamp((-1) * (startPos.x - endPos.x), -100, 100);
        Vector3 endtangent = new Vector3(endPos.x - tangent, endPos.y, 0);

        Color shadowCol = new Color(0, 0, 0, 0.06f);
        for (int i = 0; i < 3; i++)
        {//shadow
            Handles.DrawBezier(startPos, endPos, endtangent, endtangent, shadowCol, null, (i + 1) * 5);
        }
        Handles.DrawBezier(startPos, endPos, endtangent, endtangent, color, null, 1);
    }

    void RemoveNode()
    {
    }

    void OnNodeClick(Node node)
    {
        selectedNode = node;
        Event.current.Use();
    }

    void OnPortClick(Rect port)
    {
        selectedPort = port;
        Event.current.Use();
    }


    void OnDrag()
    {
        if (selectedNode == null && selectedPort == null) return;
        else if (selectedNode != null)
        {
            selectedNode.rect.position += Event.current.delta;
            Event.current.Use();
            return;
        }
        else if (selectedPort != null)
        {
            DrawBezier(selectedPort.center, Event.current.mousePosition, NodeStyle.wTextColor);
            Event.current.Use();
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
