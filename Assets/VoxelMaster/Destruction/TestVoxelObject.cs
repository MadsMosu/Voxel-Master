using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelMaster.Chunk;
using VoxelMaster.Core.Rendering;

public class TestVoxelObject {
    public Material material;
    public float size = 10;
    public Vector3Int chunkSize;
    public float isoLevel = 0f;
    public bool original;
    private Mesh mesh;
    private float voxelScale;
    MarchingCubesGPU meshGenerator;
    public VoxelChunk chunk;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    private bool needsUpdate = true;

    public void Start () {
        meshGenerator = new MarchingCubesGPU ();
        voxelScale = size / chunkSize.x;

        if (original) {
            chunk = new VoxelChunk (Vector3Int.zero, chunkSize, voxelScale, new JaggedDataStructure ());
            chunk.voxels.Init (chunkSize);
            float radius = (chunkSize.x / 3f);

            Vector3 sphere1Center = chunkSize / 2;
            GenerateSDF (sphere1Center, radius);

        }

        UpdateMesh ();
    }

    private void GenerateSDF (Vector3 center, float radius) {
        chunk.voxels.Traverse ((x, y, z, voxel) => {
            // if (chunk.voxels.GetVoxel (new Vector3Int (x, y, z)).density > isoLevel) return;
            Vector3 voxelPos = (new Vector3 (x, y, z));
            float density = Vector3.Distance (voxelPos, center) - radius;
            chunk.voxels.SetVoxel (x, y, z, new Voxel { density = -density });
        });

    }

    public Vector3 prevVelocity = Vector3.zero;

    public void UpdateMesh () {
        mesh = meshGenerator.GenerateMesh (chunk).BuildMesh ();
        needsUpdate = false;
    }

}