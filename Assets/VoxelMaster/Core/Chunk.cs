using System.Linq;
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
    private bool requiresUpdate = false;
    private bool processing = false;

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

    private void GenerateLODMeshes(Action callback)
    {
        lodMeshes = new LODMesh[lodLevels.Length];
        for (int i = 0; i < lodLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(lodLevels[i].lod, meshGenerator);
            lodMeshes[i].callback += callback;
        }
    }

    public void UpdateChunk()
    {
        if (requiresUpdate && !processing)
        {
            processing = true;
            GenerateLODMeshes(onResetMesh);
            SetOrRequestMesh();
        }
        // else
        // {
        //     int lodIndex = FindLod();
        //     if (lodIndex != prevLodIndex)
        //     {
        //         SetOrRequestMesh();
        //     }
        // }
    }

    private void onResetMesh()
    {
        SetOrRequestMesh();
        requiresUpdate = false;
        processing = false;
    }

    private int FindLod()
    {
        float viewerDistance = 3;
        int lodIndex = 0;
        if (Visibility)
        {
            for (int i = 0; i < lodLevels.Length; i++)
            {
                if (viewerDistance > lodLevels[i].distance) lodIndex = i;
                else break;
            }
        }
        return lodIndex;
    }

    private void SetOrRequestMesh()
    {
        int lodIndex = FindLod();
        LODMesh lodMesh = lodMeshes[lodIndex];
        if (lodMesh.hasMesh)
        {
            prevLodIndex = lodIndex;
            meshFilter.mesh = lodMesh.mesh;
            meshCollider.sharedMesh = lodMesh.mesh;
        }
        else
        {
            lodMesh.RequestMesh(this);
        }
    }

    private void AddChunk()
    {
        if (chunkDataReceived)
        {
            SetOrRequestMesh();
        }
    }


    private void OnChunkData(ChunkData data)
    {
        voxels = data.voxels;
        chunkDataReceived = true;
        SetOrRequestMesh();
    }

    public void Load()
    {
        GenerateLODMeshes(AddChunk);
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

    public void AddDensityInSphere(Vector3Int origin, float radius, float falloff, float amount)
    {
        for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
                for (var z = -radius; z <= radius; z++)
                {
                    var voxel = GetVoxel(Mathf.FloorToInt(origin.x + x), Mathf.FloorToInt(origin.y + y), Mathf.FloorToInt(origin.z + z));
                    amount = Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2) + Mathf.Pow(z, 2));
                    SetVoxel(Mathf.FloorToInt(origin.x + x), Mathf.FloorToInt(origin.y + y), Mathf.FloorToInt(origin.z + z), new Voxel { Density = voxel.Density + amount });
                }
    }

    public Voxel this[int x, int y, int z]
    {
        get { return GetVoxel(x, y, z); }
        set { this.SetVoxel(x, y, z, value); }
    }

    public void addDensity(Vector3Int origin, float amount)
    {
        var voxel = GetVoxel(origin.x, origin.y, origin.z);
        SetVoxel(origin.x, origin.y, origin.z, new Voxel { Density = voxel.Density + amount });
        requiresUpdate = true;
    }


    class LODMesh
    {
        int lod;
        private MeshGenerator meshGenerator;
        public Mesh mesh;

        bool hasRequestedMesh;
        public bool hasMesh;

        public event Action callback;

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

            callback();
        }
    }
}
