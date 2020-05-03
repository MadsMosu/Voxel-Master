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

        private void Start() {
            meshRenderer = GetComponent<MeshRenderer>();
        }



        FastNoise noise = new FastNoise();
        public float noiseScale = 0.02f;
        public void GenerateNoise() {
            noise.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Euclidean);
            noise.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Sub);


            Texture2D tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++) {
                    var n = noise.GetCellular(x / noiseScale, y / noiseScale);
                    var c = new Color(1 * n, 1 * n, 1 * n);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();

            meshRenderer.sharedMaterial.SetTexture("_BaseColorMap", tex);

        }

    }
}
