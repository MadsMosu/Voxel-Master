using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.Core.Rendering;

[RequireComponent (typeof (MeshFilter), typeof (MeshRenderer), typeof (MeshCollider))]
public class VoxelObject : MonoBehaviour {
    public Material material;
    public float size = 10;
    public Vector3Int chunkSize;
    public float isoLevel = 0f;
    public bool original;
    private Mesh mesh;
    private float voxelScale;
    MarchingCubesGPU meshGenerator;
    public VoxelChunk chunk;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    private bool needsUpdate = true;

    void Start () {
        meshGenerator = new MarchingCubesGPU ();
        voxelScale = 1f;

        float posX = transform.position.x + (chunkSize.x * voxelScale);
        float posY = transform.position.y + (chunkSize.y * voxelScale);
        float posZ = transform.position.z + (chunkSize.z * voxelScale);

        if (original) {
            chunk = new VoxelChunk (new Vector3Int ((int) posX, (int) posY, (int) posZ), chunkSize, voxelScale, new SimpleDataStructure ());
            chunk.voxels.Init (chunkSize);
            float centerX = Mathf.Lerp (transform.position.x, transform.position.x + size, 0.5f);
            float centerY = Mathf.Lerp (transform.position.y, transform.position.y + size, 0.5f);
            float centerZ = Mathf.Lerp (transform.position.z, transform.position.z + size, 0.5f);
            Vector3 center = new Vector3 (centerX, centerY, centerZ);
            float radius = (size / 4);

            Vector3 sphere1Center = transform.position + chunkSize / 2;
            sphere1Center += Vector3.right * radius * 1.4f;
            sphere1Center.y -= radius;

            Vector3 sphere2Center = transform.position + chunkSize / 2;
            sphere2Center += Vector3.left * radius * 1.4f;
            sphere2Center.y -= radius;

            GenerateSDF (sphere1Center, radius);
            GenerateSDF (sphere2Center, radius);

        }

        meshFilter = gameObject.GetComponent<MeshFilter> ();
        meshRenderer = gameObject.GetComponent<MeshRenderer> ();
        meshCollider = gameObject.GetComponent<MeshCollider> ();
        meshRenderer.material = material;

        UpdateMesh ();

    }

    void Update () {
        // if (needsUpdate) {
        //     UpdateMesh ();
        // }

    }

    public void Slice (Vector3 point, float amount) {
        Vector3 transformPoint = transform.InverseTransformPoint (point);
        Vector3Int voxelCoord = new Vector3Int (
            Mathf.FloorToInt (MapToVoxelSpace (0, size, 0, chunkSize.x, Mathf.Abs (transformPoint.x))),
            Mathf.FloorToInt (MapToVoxelSpace (0, size, 0, chunkSize.y, Mathf.Abs (transformPoint.y))),
            Mathf.FloorToInt (MapToVoxelSpace (0, size, 0, chunkSize.z, Mathf.Abs (transformPoint.z)))
        );
        float oldDensity = chunk.voxels.GetVoxel (voxelCoord).density;
        chunk.voxels.SetVoxel (voxelCoord, new Voxel { density = oldDensity + amount });

    }

    private void GenerateSDF (Vector3 center, float radius) {
        chunk.voxels.Traverse ((x, y, z, voxel) => {
            if (chunk.voxels.GetVoxel (new Vector3Int (x, y, z)).density > isoLevel) return;
            Vector3 voxelPos = (new Vector3 (x, y, z) * voxelScale);
            float density = Vector3.Distance (voxelPos, center) - radius;

            chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel { density = -density, materialIndex = 0 });
        });

    }

    private void UpdateMesh () {
        mesh = meshGenerator.GenerateMesh (chunk).BuildMesh ();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        needsUpdate = false;
    }

    private float MapToVoxelSpace (float a1, float a2, float b1, float b2, float s) => b1 + (s - a1) * (b2 - b1) / (a2 - a1);

    private void OnDrawGizmosSelected () {
        if (chunk == null || chunk.voxels == null) return;
        var gizmoColors = new Color[] {
            // new Color (0, 1, 0, .25f),
            Color.clear,
            Color.blue,
            Color.yellow,
        };
        chunk.voxels.Traverse ((x, y, z, v) => {
            Gizmos.color = gizmoColors[v.materialIndex];
            Gizmos.DrawWireSphere (transform.TransformPoint (new Vector3 (x * voxelScale, y * voxelScale, z * voxelScale)), .25f);
        });
        // Gizmos.color = Color.cyan;
        // foreach (var voxelPos in voxelPositions) {
        //     Gizmos.DrawSphere (voxelPos, .1f);
        // }
        // Gizmos.color = Color.magenta;
        // foreach (var bound in bounds) {
        //     Debug.Log (bound);
        //     Gizmos.DrawWireCube (bound.center, bound.size);
        // };
        // Vector3 size = chunk.size;
        // size /= 2;
        // BoundsInt bounds = new BoundsInt (new Vector3Int ((int) transform.position.x, (int) transform.position.y, (int) transform.position.z), new Vector3Int ((int) size.x, (int) size.y, (int) size.z));
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube (transform.position + chunkSize / 2, chunkSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube (transform.position + VoxelSplitter.voxelSpaceBound.center, VoxelSplitter.voxelSpaceBound.size);

        Gizmos.color = Color.green;
        foreach (var pos in VoxelDataStructure.staticVoxels) {
            Gizmos.DrawWireSphere (transform.position + pos, 0.20f);

        }
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

    public void Union (int index, int[] ints) {
        for (int i = 0; i < ints.Length; i++) Union (ints[i], index);
    }
}