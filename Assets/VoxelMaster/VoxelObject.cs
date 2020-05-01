using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.Core.Rendering;

namespace VoxelMaster {
    public class VoxelObject : MonoBehaviour {
        public Material material;
        public float size = 10;
        public int chunkSize = 16;
        public float isoLevel = 0f;
        public bool original = true;
        private Mesh mesh;
        private float voxelScale;
        MarchingCubesGPU meshGenerator;
        private int splitLevel = 0;
        private int maxSplitLevel = 5;
        VoxelChunk chunk;
        SimpleDataStructure inheritedVoxels;
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        MeshCollider meshCollider;

        void Start () {
            meshGenerator = new MarchingCubesGPU ();
            voxelScale = size / chunkSize;

            float posX = transform.position.x + (chunkSize * voxelScale);
            float posY = transform.position.y + (chunkSize * voxelScale);
            float posZ = transform.position.z + (chunkSize * voxelScale);
            chunk = new VoxelChunk (new Vector3Int ((int) posX, (int) posY, (int) posZ), chunkSize, voxelScale, new SimpleDataStructure ());

            if (original) {
                float centerX = Mathf.Lerp (transform.position.x, transform.position.x + size, 0.5f);
                float centerY = Mathf.Lerp (transform.position.y, transform.position.y + size, 0.5f);
                float centerZ = Mathf.Lerp (transform.position.z, transform.position.z + size, 0.5f);
                Vector3 center = new Vector3 (centerX, centerY, centerZ);
                float radius = (size / 2) * 0.8f;

                // Vector3 sphere1Pos = new Vector3 (
                //     Mathf.Lerp (transform.position.x, transform.position.x + centerX / 2, 0.5f),
                //     Mathf.Lerp (transform.position.y, transform.position.y + centerY / 2, 0.5f),
                //     Mathf.Lerp (transform.position.z, transform.position.z + centerZ / 2, 0.5f)
                // );
                Vector3 sphere1Center = new Vector3 (
                    Mathf.Lerp (transform.position.x, transform.position.x + centerX * 0.5f, 0.5f),
                    Mathf.Lerp (transform.position.y, transform.position.y + centerY * 0.5f, 0.5f),
                    Mathf.Lerp (transform.position.z, transform.position.z + centerZ * 0.5f, 0.5f)
                );

                // Vector3 sphere2Pos = new Vector3 (
                //     Mathf.Lerp (transform.position.x, transform.position.x + centerX + centerX * 0.5f, 0.5f),
                //     Mathf.Lerp (transform.position.y, transform.position.y + centerY + centerY * 0.5f, 0.5f),
                //     Mathf.Lerp (transform.position.z, transform.position.z + centerZ + centerZ * 0.5f, 0.5f)
                // );
                Vector3 sphere2Center = new Vector3 (
                    Mathf.Lerp (centerX, transform.position.x + centerX + centerX * 0.5f, 0.5f),
                    Mathf.Lerp (centerY, transform.position.y + centerY + centerY * 0.5f, 0.5f),
                    Mathf.Lerp (centerZ, transform.position.z + centerZ + centerZ * 0.5f, 0.5f)
                );

                GenerateSDF (transform.position, sphere1Center, radius / 6);
                GenerateSDF (center, sphere2Center, radius / 6);

            } else {

            }

            mesh = meshGenerator.GenerateMesh (chunk).BuildMesh ();
            meshFilter = gameObject.AddComponent<MeshFilter> ();
            meshRenderer = gameObject.AddComponent<MeshRenderer> ();
            meshCollider = gameObject.AddComponent<MeshCollider> ();

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
            meshRenderer.material = material;
            // gameObject.AddComponent<Rigidbody> ();

            Islands ();
        }

        void Update () {

        }

        public void Slice (Vector3 point, float amount) {
            Vector3 transformPoint = transform.InverseTransformPoint (point);
            Vector3Int voxelCoord = new Vector3Int (
                Mathf.FloorToInt (MapToVoxelSpace (0, size, 0, chunkSize, Mathf.Abs (transformPoint.x))),
                Mathf.FloorToInt (MapToVoxelSpace (0, size, 0, chunkSize, Mathf.Abs (transformPoint.y))),
                Mathf.FloorToInt (MapToVoxelSpace (0, size, 0, chunkSize, Mathf.Abs (transformPoint.z)))
            );
            float oldDensity = chunk.voxels.GetVoxel (voxelCoord).density;
            chunk.voxels.SetVoxel (voxelCoord, new Voxel { density = oldDensity + amount });
            UpdateMesh ();

        }

        private void Islands () {
            DisjointSet linked = new DisjointSet (chunk.voxels.ToArray ().Length);
            int[] labels = new int[chunk.voxels.ToArray ().Length];
            int currentLabel = 1;

            chunk.voxels.Traverse ((x, y, z, voxel) => {
                if (voxel.density < 0f) return;

                List<int> neighborLabels = new List<int> ();
                for (int nx = -1; nx <= 1; nx++)
                    for (int ny = -1; ny <= 1; ny++)
                        for (int nz = -1; nz <= 1; nz++) {
                            if (nx == 0 && ny == 0 && nz == 0) continue;
                            if (
                                (x + nx < 0 || x + nx >= chunkSize) ||
                                (y + ny < 0 || y + ny >= chunkSize) ||
                                (z + nz < 0 || z + nz >= chunkSize)
                            ) continue;
                            if (chunk.voxels.GetVoxel (new Vector3Int (x + nx, y + ny, z + nz)).density >= isoLevel) {
                                neighborLabels.Add (labels[Util.Map3DTo1D (new Vector3Int (x + nx, y + ny, z + nz), chunkSize)]);
                            }
                        }
                Debug.Log (neighborLabels.Count);
                if (neighborLabels.Count == 0) {
                    linked.MakeSet (currentLabel);
                    labels[Util.Map3DTo1D (new Vector3Int (x, y, z), chunkSize)] = currentLabel;
                    voxel.materialIndex = (byte) currentLabel;
                    currentLabel++;
                } else {
                    int smallestLabel = neighborLabels.OrderBy (label => label).First ();
                    labels[Util.Map3DTo1D (new Vector3Int (x, y, z), chunkSize)] = smallestLabel;
                    voxel.materialIndex = (byte) smallestLabel;

                    for (int n = 0; n < neighborLabels.Count; n++) {
                        int neighborLabel = neighborLabels[n];
                        linked.Union (currentLabel, neighborLabel);
                    }
                }
            });

            for (int i = 0; i < labels.Length; i++) {
                labels[i] = linked.Find (labels[i]);
            }

            Debug.Log (currentLabel);
            UpdateMesh ();

        }

        private int GetNeighborIndex (Vector3Int neighborPos) {
            neighborPos += Vector3Int.one;
            var index = Util.Map3DTo1D (neighborPos, 3);
            if (index >= 13) {
                index -= 1;
            }
            return index;
        }

        private void GenerateSDF (Vector3 pos, Vector3 center, float radius) {
            chunk.voxels.Traverse ((x, y, z, voxel) => {
                if (chunk.voxels.GetVoxel (new Vector3Int (x, y, z)).density > isoLevel) return;
                Vector3 voxelPos = pos + (new Vector3 (x, y, z) * voxelScale);
                float density = SDFSphere (voxelPos, center, radius);

                chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = -density, materialIndex = 0 });
            });

        }

        private void UpdateMesh () {
            mesh = meshGenerator.GenerateMesh (chunk).BuildMesh ();
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        private float MapToVoxelSpace (float a1, float a2, float b1, float b2, float s) => b1 + (s - a1) * (b2 - b1) / (a2 - a1);

        private float SDFSphere (Vector3 pos, Vector3 center, float radius) {
            return Vector3.Distance (pos, center) - radius;
        }
    }

    class DisjointSet {
        //https://stackoverflow.com/questions/46383172/disjoint-set-implementation-in-c-sharp
        int[] parent;
        int[] rank; // height of tree

        public DisjointSet (int arrayLength) {
            parent = new int[arrayLength];
            rank = new int[arrayLength];
        }

        public void MakeSet (int i) {
            parent[i] = i;
        }

        public int Find (int i) {
            while (i != parent[i]) // If i is not root of tree we set i to his parent until we reach root (parent of all parents)
            {
                i = parent[i];
            }
            return i;
        }

        // Path compression, O(log*n). For practical values of n, log* n <= 5
        public int FindPath (int i) {
            if (i != parent[i]) {
                parent[i] = FindPath (parent[i]);
            }
            return parent[i];
        }

        public void Union (int i, int j) {
            int i_id = Find (i); // Find the root of first tree (set) and store it in i_id
            int j_id = Find (j); // // Find the root of second tree (set) and store it in j_id

            if (i_id == j_id) // If roots are equal (they have same parents) than they are in same tree (set)
            {
                return;
            }

            if (rank[i_id] > rank[j_id]) // If height of first tree is larger than second tree
            {
                parent[j_id] = i_id; // We hang second tree under first, parent of second tree is same as first tree
            } else {
                parent[i_id] = j_id; // We hang first tree under second, parent of first tree is same as second tree
                if (rank[i_id] == rank[j_id]) // If heights are same
                {
                    rank[j_id]++; // We hang first tree under second, that means height of tree is incremented by one
                }
            }
        }
        public void Union (int i, int[] j) {

        }
    }
}