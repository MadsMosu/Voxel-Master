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
    new Rigidbody rigidbody;
    private bool needsUpdate = true;

    void Start () {
        meshGenerator = new MarchingCubesGPU ();
        voxelScale = size / chunkSize.x;

        if (original) {
            chunk = new VoxelChunk (Vector3Int.zero, chunkSize, voxelScale, new SimpleDataStructure ());
            chunk.voxels.Init (chunkSize);
            float radius = (chunkSize.x / 3f);

            Vector3 sphere1Center = chunkSize / 2;
            GenerateSDF (sphere1Center, radius);

        }

        meshFilter = gameObject.GetComponent<MeshFilter> ();
        meshRenderer = gameObject.GetComponent<MeshRenderer> ();
        meshCollider = gameObject.GetComponent<MeshCollider> ();
        meshRenderer.material = material;
        rigidbody = GetComponent<Rigidbody> ();

        UpdateMesh ();

    }

    private void GenerateSDF (Vector3 center, float radius) {
        chunk.voxels.Traverse ((x, y, z, voxel) => {
            if (chunk.voxels.GetVoxel (new Vector3Int (x, y, z)).density > isoLevel) return;
            Vector3 voxelPos = (new Vector3 (x, y, z));
            float density = Vector3.Distance (voxelPos, center) - radius;

            chunk.voxels.SetVoxel (new Vector3Int (x, y, z), new Voxel (-density));
        });

    }

    public Vector3 prevVelocity = Vector3.zero;
    private void FixedUpdate () {
        prevVelocity = rigidbody.velocity;
    }

    private void OnCollisionEnter (Collision collision) {
        if (collision.impulse.magnitude > 25) {
            Debug.Log (rigidbody.velocity);
            Debug.Log (prevVelocity);
            VoxelSplitter.Split (this);
        }
    }

    public void UpdateMesh () {
        mesh = meshGenerator.GenerateMesh (chunk).BuildMesh ();
        meshFilter.mesh = mesh;
        meshCollider.convex = true;
        meshCollider.sharedMesh = mesh;
        needsUpdate = false;
    }

    private void OnDrawGizmosSelected () {
        if (chunk == null || chunk.voxels == null) return;
        //var gizmoColors = new Color[] {
        //    // new Color (0, 1, 0, .25f),
        //    Color.clear,
        //    Color.blue,
        //    Color.yellow,
        //};
        //chunk.voxels.Traverse((x, y, z, v) => {
        //    Gizmos.color = gizmoColors[v.materialIndex];
        //    Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(x * voxelScale, y * voxelScale, z * voxelScale)), .25f);
        //});
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
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube ((new Vector3 (chunkSize.x, chunkSize.y, chunkSize.z) * voxelScale) / 2, new Vector3 (chunkSize.x, chunkSize.y, chunkSize.z) * voxelScale);
        //Gizmos.color = Color.cyan;
        //Gizmos.DrawWireCube(transform.position + VoxelSplitter.voxelSpaceBound.center, VoxelSplitter.voxelSpaceBound.size);

        //Gizmos.color = Color.green;
        //foreach (var pos in VoxelDataStructure.staticVoxels) {
        //    Gizmos.DrawWireSphere(transform.position + pos, 0.20f);

        //}
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