using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoxelMaster.Destruction {

    [ExecuteInEditMode]
    public class NoiseVisualizer : MonoBehaviour {

        MeshRenderer meshRenderer;

        List<Vector3> samples;

        private void Start() {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        FastNoise noise = new FastNoise();
        [Range(.5f, 4f)]
        public float noiseScale = 4f;

        [Range(1f, 8f)]
        public float a = 1f;
        public float b = 1f;
        public float c = 1f;
        public void GenerateNoise() {

            samples = PoissonSampler.GeneratePoints(new Vector3Int(17, 17, 17), noiseScale, Vector3.zero, 50);

        }

        void OnDrawGizmos() {
            Gizmos.color = Color.magenta;
            if (VoxelSplitter.lastSplitSamples != null) {
                foreach (var sample in VoxelSplitter.lastSplitSamples) {
                    Gizmos.DrawSphere(sample, 0.20f);
                }
            }

        }

    }
}