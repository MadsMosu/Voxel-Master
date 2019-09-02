

using System;
using UnityEngine;

public class Chunk
{

    public bool Visibility
    {
        get
        {
            return meshRenderer.enabled;
        }
        set
        {
            meshRenderer.enabled = value;
        }
    }

    public Vector3Int coords;
    public int size;
    public int lod;

    LODLevel[] lodLevels;
    LODMesh[] lodMeshes;
    private int prevLodIndex;
    private bool chunkDataReceived;
    private bool meshDataReceived;

    private WorldGenerator worldGenerator;
    private MeshGenerator MeshGenerator;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    private Transform viewer;

    private Voxel[,,] voxels;

    public Voxel[,,] Voxels
    {
        get { return voxels; }
    }


    public Chunk(Vector3Int coordinates, int size, WorldGenerator worldGenerator, MeshGenerator meshGenerator, Material mat, LODLevel[] lodLevels, Transform viewer)
    {
        coords = coordinates;
        this.size = size;

        this.worldGenerator = worldGenerator;
        this.MeshGenerator = meshGenerator;

        var go = new GameObject($"Chunk({coords})");
        go.transform.position = coords * size;
        meshFilter = go.AddComponent<MeshFilter>();
        meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material = mat;

        this.lodLevels = lodLevels;
        lodMeshes = new LODMesh[lodLevels.Length];
        for (int i = 0; i < lodLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(lodLevels[i].lod, meshGenerator);
            lodMeshes[i].updateCallback += UpdateChunk;
        }

        this.viewer = viewer;

    }

    private void UpdateChunk()
    {
        if (chunkDataReceived)
        {
            float viewerDistance = Vector3Int.Distance(coords, new Vector3Int(
                (int)viewer.position.x,
                (int)viewer.position.y,
                (int)viewer.position.z
            ));

            if (Visibility == true)
            {
                int lodIndex = 0;

                for (int i = 0; i < lodLevels.Length; i++)
                {
                    if (viewerDistance > lodLevels[i].distance) lodIndex = i + 1;
                    else break;
                }

                if (lodIndex != prevLodIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        prevLodIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    }
                    else
                    {
                        lodMesh.RequestMesh(this);
                    }
                }
            }
        }
    }

    private void OnChunkData(ChunkData data)
    {
        voxels = data.voxels;
        chunkDataReceived = true;
        UpdateChunk();
    }

    public void Load()
    {
        worldGenerator.RequestChunkData(this, OnChunkData);
    }


    public void SetVoxel(Vector3Int p, Voxel v)
    {
        voxels[p.x, p.y, p.z] = v;
    }


    public void SetVoxels(Voxel[,,] voxels)
    {
        this.voxels = voxels;
    }

    public void AddDensityInSphere(Vector3 origin, float radius, float falloff)
    {
        //TODO:
    }

    class LODMesh
    {
        int lod;
        private MeshGenerator meshGenerator;
        public Mesh mesh;

        bool hasRequestedMesh;
        public bool hasMesh;

        public event Action updateCallback;

        public LODMesh(int lod, MeshGenerator meshGenerator)
        {
            this.lod = lod;
            this.meshGenerator = meshGenerator;
        }

        internal void RequestMesh(Chunk c)
        {
            hasRequestedMesh = true;
            meshGenerator.RequestMeshData(c, OnMeshDataReceived);
        }

        private void OnMeshDataReceived(MeshData data)
        {
            hasMesh = true;
            mesh = data.CreateMesh();

            updateCallback();
        }
    }
}
