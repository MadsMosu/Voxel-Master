using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugGUI : MonoBehaviour {

    public static void AddVariable(String label, Func<object> valueCallback) {
        variables[label] = valueCallback;
    }

    private static Dictionary<string, Func<object>> variables = new Dictionary<string, Func<object>>();

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    void OnGUI() {
        GUILayout.BeginVertical("box");
        foreach (var variable in variables) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(variable.Key);
            GUILayout.Label(variable.Value().ToString());
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }
}
