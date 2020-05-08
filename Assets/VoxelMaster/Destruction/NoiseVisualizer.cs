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

        private void Start () {
            meshRenderer = GetComponent<MeshRenderer> ();
        }

        FastNoise noise = new FastNoise ();
        [Range (2f, 10f)]
        public float noiseScale = 4f;

        [Range (1f, 8f)]
        public float a = 1f;
        public float b = 1f;
        public float c = 1f;
        public void GenerateNoise () {
            samples = PoissonSampler.GeneratePoints (noiseScale, new Chunk.VoxelChunk (new Vector3Int ((int) transform.position.x, (int) transform.position.y, (int) transform.position.z), new Vector3Int (17, 17, 17), 1f, new SimpleDataStructure ()), Vector3.zero);
            // Debug.Log (samples.Count ())

        }

        void OnDrawGizmos () {
            Gizmos.color = Color.magenta;
            if (samples != null) {
                foreach (var sample in samples) {
                    Gizmos.DrawSphere (sample, 0.20f);
                }
            }

        }

    }
}