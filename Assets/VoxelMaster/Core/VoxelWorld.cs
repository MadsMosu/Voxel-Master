using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MarchingCubes;

public class VoxelWorld : MonoBehaviour
{

    public Transform target;
    public int searchRadius = 5;

    public Material material;

    public int chunkSize = 16;

    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    WorldGenerator worldGenerator = new WorldGenerator(new WorldSettings());
    MeshGenerator meshGenerator = new MeshGenerator(new MeshSettings());

    private void Update()
    {
        UpdateChunks();

        worldGenerator.MainThreadUpdate();
        meshGenerator.MainThreadUpdate();

    }

    void UpdateChunks()
    {

        var targetCoords = new Vector3Int(
            Mathf.RoundToInt(target.position.x / chunkSize),
            Mathf.RoundToInt(target.position.y / chunkSize),
            Mathf.RoundToInt(target.position.z / chunkSize)
        );

        for (int x = -searchRadius; x < searchRadius; x++)
            for (int y = -searchRadius; y < searchRadius; y++)
                for (int z = -searchRadius; z < searchRadius; z++)
                {
                    var coord = targetCoords + new Vector3Int(x, y, z);
                    if (!chunks.ContainsKey(coord))
                    {
                        var c = new Chunk(coord, chunkSize);
                        chunks.Add(coord, c);
                        worldGenerator.RequestChunkData(c, OnChunkData);
                    }
                }

    }

    void OnChunkData(ChunkData data)
    {
        var chunk = chunks[data.coords];
        chunk.SetVoxels(data.voxels);
        meshGenerator.RequestMeshData(chunk, OnMeshData);
    }


    void OnMeshData(MeshData data)
    {
        var mesh = new Mesh();
        mesh.vertices = data.vertices;
        mesh.triangles = data.triangles;
        mesh.RecalculateNormals();

        var go = new GameObject($"Chunk({data.coords})");
        go.transform.position = data.coords * chunkSize;
        var meshFilter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = material;


    }




    //private void OnDrawGizmos()
    //{
    //    foreach (KeyValuePair<Vector3Int, Chunk> keyValue in chunks)
    //    {
    //        var chunk = keyValue.Value;

    //        for (int x = 0; x < chunk.Voxels.GetLength(0); x++)
    //            for (int y = 0; y < chunk.Voxels.GetLength(1); y++)
    //                for (int z = 0; z < chunk.Voxels.GetLength(2); z++)
    //                {
    //                    Gizmos.color = Color.HSVToRGB(0, 0, chunk.Voxels[x, y, z].Density);
    //                    Gizmos.DrawWireSphere(new Vector3(x, y, z), 0.03f);
    //                }


    //    }
    //}
}
