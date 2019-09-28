using System.Collections.Generic;
using UnityEngine;
using XNodeEditor;

[CustomNodeGraphEditor(typeof(TerrainGraph), "TerrainGraph.Settings")]
public class MyGraphEditor : NodeGraphEditor
{

    public override NodeEditorPreferences.Settings GetDefaultPreferences()
    {
        var colorSaturation = .6f;
        var colorValue = .8f;
        return new NodeEditorPreferences.Settings()
        {
            gridBgColor = new Color(.2f, .2f, .2f),
            gridLineColor = new Color(.4f, .4f, .4f),
            typeColors = new Dictionary<string, Color>() {
            { typeof(float).PrettyName(), Color.HSVToRGB(0.6f ,colorSaturation,colorValue) },
        }
        };
    }

}