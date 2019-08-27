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


    private void Update()
    {
        var targetCoords = new Vector3Int(
            Mathf.RoundToInt(target.position.x / chunkSize),
            Mathf.RoundToInt(target.position.y / chunkSize),
            Mathf.RoundToInt(target.position.z / chunkSize)
        );

        for (int x = -5; x < 5; x++)
            for (int y = -5; y < 5; y++)
                for (int z = -5; z < 5; z++)
                {
                    var coord = targetCoords + new Vector3Int(x, y, z);
                    if (!chunks.ContainsKey(coord))
                    {
                        var c = new Chunk(coord, chunkSize);
                        chunks.Add(coord, c);
                        GenerateMesh(c);
                    }
                }


    }


    void GenerateMesh(Chunk chunk)
    {


        List<Triangle> triangles;
        MarchingCubes.GenerateMesh(chunk, out triangles);

        var verts = new List<Vector3>();
        var tris = new List<int>();

        int triIndex = 0;
        foreach (var triangle in triangles)
        {
            verts.Add(triangle.a);
            tris.Add(triIndex + 2);
            verts.Add(triangle.b);
            tris.Add(triIndex + 1);
            verts.Add(triangle.c);
            tris.Add(triIndex);

            triIndex += 3;

        }


        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);

        mesh.RecalculateNormals();

        var go = new GameObject("Chunk");
        go.transform.position = chunk.chunkCoordinates * chunkSize;
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
