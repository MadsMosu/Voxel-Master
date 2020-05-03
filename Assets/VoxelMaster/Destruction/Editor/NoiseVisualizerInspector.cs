using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace VoxelMaster.Destruction {

    [CustomEditor(typeof(NoiseVisualizer))]
    public class NoiseVisualizerInspector : Editor {

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            NoiseVisualizer noiseVisualizer = (NoiseVisualizer)target;

            if (GUILayout.Button("Generate")) noiseVisualizer.GenerateNoise();


            if (GUI.changed) {
                EditorUtility.SetDirty(target);
                noiseVisualizer.GenerateNoise();
            }
        }

    }
}
