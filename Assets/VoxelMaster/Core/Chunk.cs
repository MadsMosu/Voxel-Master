

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
    public float voxelSize;
    public int lod;

    LODLevel[] lodLevels;
    LODMesh[] lodMeshes;
    private int prevLodIndex = -1;
    private bool chunkDataReceived;
    private bool meshDataReceived;

    private WorldGenerator worldGenerator;
    private MeshGenerator meshGenerator;

    private MeshCollider meshCollider;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    private Transform viewer;

    private Voxel[] voxels;

    public Voxel[] Voxels
    {
        get { return voxels; }
    }


    public Chunk(Vector3Int coordinates, int size, float voxelSize, WorldGenerator worldGenerator, MeshGenerator meshGenerator, Material mat, LODLevel[] lodLevels, Transform viewer)
    {
        coords = coordinates;
        this.size = size + 1;
        this.voxelSize = voxelSize;

        this.worldGenerator = worldGenerator;
        this.meshGenerator = meshGenerator;

        var go = new GameObject($"Chunk({coords})");

        go.transform.position = coords * size;

        meshFilter = go.AddComponent<MeshFilter>();
        meshRenderer = go.AddComponent<MeshRenderer>();
        meshCollider = go.AddComponent<MeshCollider>();
        meshRenderer.material = mat;

        this.lodLevels = lodLevels;


        this.viewer = viewer;
    }

    public void GenerateLODMeshes()
    {
        lodMeshes = new LODMesh[lodLevels.Length];
        for (int i = 0; i < lodLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(lodLevels[i].lod, meshGenerator);
            lodMeshes[i].updateCallback += UpdateChunk;
        }
    }

    private void UpdateChunk()
    {
        if (chunkDataReceived)
        {
            float viewerDistance = 3;

            if (Visibility == true)
            {
                int lodIndex = 0;

                for (int i = 0; i < lodLevels.Length; i++)
                {
                    if (viewerDistance > lodLevels[i].distance) lodIndex = i;
                    else break;
                }

                if (lodIndex != prevLodIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        prevLodIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                        // meshCollider.sharedMesh = lodMesh.mesh;
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

    public Voxel GetVoxel(int x, int y, int z)
    {
        return voxels[MapIndexTo1D(x, y, z)];
    }

    public void SetVoxel(int x, int y, int z, Voxel v)
    {
        voxels[MapIndexTo1D(x, y, z)] = v;
    }


    public void SetVoxels(Voxel[] voxels)
    {
        this.voxels = voxels;
    }


    public int MapIndexTo1D(int x, int y, int z)
    {
        return (x + size * (y + size * z));
    }

    public void AddDensityInSphere(Vector3 origin, float radius, float falloff)
    {
        //TODO:
    }

    public Voxel this[int x, int y, int z]
    {
        get { return GetVoxel(x, y, z); }
        set { this.SetVoxel(x, y, z, value); }
    }

    public void addDensity(Vector3Int origin, float amount)
    {
        for (int x = -1; x < 1; x++)
            for (int y = -1; y < 1; y++)
                for (int z = -1; z < 1; z++)
                {
                    try
                    {
                        var voxel = GetVoxel(origin.x + x, origin.y + y, origin.z + z);
                        SetVoxel(origin.x + x, origin.y + y, origin.z + z, new Voxel { Density = voxel.Density + amount });
                        Debug.Log("IT WORKS");
                    }
                    catch (IndexOutOfRangeException e)
                    {

                    }
                }
        GenerateLODMeshes();
        UpdateChunk();
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
