using System.Collections.Generic;
using UnityEngine;

//https://gist.github.com/hiroakioishi/382a6ecbf741c5e0d463
public class PoissonSampler {
    private const int k = 30; // Maximum number of attempts before marking a sample as inactive.

    private readonly Vector3 cube;
    private readonly float radius2; // radius squared
    private readonly float cellSize;
    private Vector3[, , ] grid;
    private List<Vector3> activeSamples = new List<Vector3> ();

    /// Create a sampler with the following parameters:
    ///
    /// width:  each sample's x coordinate will be between [0, width]
    /// height: each sample's y coordinate will be between [0, height]
    /// depth:  each sample's z coordinate will be between [0. depth]
    /// radius: each sample will be at least `radius` units away from any other sample, and at most 2 * `radius`.
    public PoissonSampler (float width, float height, float depth, float radius) {
        cube = new Vector3 (width, height, depth);
        radius2 = radius * radius;
        cellSize = radius / Mathf.Sqrt (3);
        grid = new Vector3[Mathf.CeilToInt (width / cellSize),
            Mathf.CeilToInt (height / cellSize),
            Mathf.CeilToInt (depth / cellSize)];
        Debug.Log (grid.GetLength (0));
        Debug.Log (grid.GetLength (1));
        Debug.Log (grid.GetLength (2));
    }

    /// Return a lazy sequence of samples. You typically want to call this in a foreach loop, like so:
    ///   foreach (Vector3 sample in sampler.Samples()) { ... }
    public IEnumerable<Vector3> Samples () {
        // First sample is choosen randomly
        yield return AddSample (new Vector3 (Random.value * cube.x, Random.value * cube.y, Random.value * cube.z));

        while (activeSamples.Count > 0) {

            // Pick a random active sample
            int i = (int) Random.value * activeSamples.Count;
            Vector3 sample = activeSamples[i];

            // Try `k` random candidates between [radius, 2 * radius] from that sample.
            bool found = false;
            for (int j = 0; j < k; ++j) {

                // Generate random point around sample.
                Vector3 candidate = GenerateRandomPointAround (sample, Random.value * 3 * radius2 + radius2);

                // Accept candidates if it's inside the rect and farther than 2 * radius to any existing sample.
                if (IsContains (candidate, cube) &&
                    IsFarEnough (candidate)) {
                    found = true;
                    yield return AddSample (candidate);
                    break;
                }
            }

            // If we couldn't find a valid candidate after k attempts, remove this sample from the active samples queue
            if (!found) {
                activeSamples[i] = activeSamples[activeSamples.Count - 1];
                activeSamples.RemoveAt (activeSamples.Count - 1);
            }
        }
    }

    private bool IsContains (Vector3 v, Vector3 area) {
        if (v.x >= 0 && v.x < area.x &&
            v.y >= 0 && v.y < area.y &&
            v.z >= 0 && v.z < area.z) {
            return true;
        } else {
            return false;
        }
    }

    private Vector3 GenerateRandomPointAround (Vector3 point, float minDist) { //non-uniform, leads to denser packing.
        float r1 = Random.value; //random point between 0 and 1
        float r2 = Random.value;
        float r3 = Random.value;
        //random radius between mindist and 2* mindist
        float radius = minDist * (r1 + 1);
        //random angle
        float angle1 = 2.0f * Mathf.PI * r2;
        float angle2 = 2.0f * Mathf.PI * r3;
        //the new point is generated around the point (x, y, z)
        float newX = point.x + radius * Mathf.Cos (angle1) * Mathf.Sin (angle2);
        float newY = point.y + radius * Mathf.Sin (angle1) * Mathf.Sin (angle2);
        float newZ = point.z + radius * Mathf.Cos (angle2);

        return new Vector3 (newX, newY, newZ);
    }

    private bool IsFarEnough (Vector3 sample) {
        GridPos pos = new GridPos (sample, cellSize);

        int xmin = Mathf.Max (pos.x - 2, 0);
        int ymin = Mathf.Max (pos.y - 2, 0);
        int zmin = Mathf.Max (pos.z - 2, 0);
        int xmax = Mathf.Min (pos.x + 2, grid.GetLength (0) - 1);
        int ymax = Mathf.Min (pos.y + 2, grid.GetLength (1) - 1);
        int zmax = Mathf.Min (pos.z + 2, grid.GetLength (2) - 1);

        for (int z = zmin; z <= zmax; z++) {
            for (int y = ymin; y <= ymax; y++) {
                for (int x = xmin; x <= xmax; x++) {
                    Vector3 s = grid[x, y, z];
                    if (s != Vector3.zero) {
                        Vector3 d = s - sample;
                        if (d.x * d.x + d.y * d.y + d.z * d.z < radius2) return false;
                    }
                }
            }
        }

        return true;

    }

    /// Adds the sample to the active samples queue and the grid before returning it
    private Vector3 AddSample (Vector3 sample) {
        activeSamples.Add (sample);
        GridPos pos = new GridPos (sample, cellSize);
        grid[pos.x, pos.y, pos.z] = sample;
        return sample;
    }

    /// Helper struct to calculate the x and y indices of a sample in the grid
    private struct GridPos {
        public int x;
        public int y;
        public int z;

        public GridPos (Vector3 sample, float cellSize) {
            x = (int) (sample.x / cellSize);
            y = (int) (sample.y / cellSize);
            z = (int) (sample.z / cellSize);
        }
    }
}