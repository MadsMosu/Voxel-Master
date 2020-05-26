using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoxelMaster;
using VoxelMaster.Chunk;

public class BenchmarkTests {

    int cycles = 1000;
    Vector3Int chunkSize32 = new Vector3Int (32, 32, 32);
    Vector3Int chunkSize16 = new Vector3Int (16, 16, 16);
    Vector3Int chunkSize8 = new Vector3Int (8, 8, 8);
    Vector3Int chunkSize4 = new Vector3Int (4, 4, 4);
    float voxelScale = 1f;
    VoxelDataStructure dataStructure32 = new SimpleDataStructure ();
    VoxelDataStructure dataStructure16 = new SimpleDataStructure ();
    VoxelDataStructure dataStructure8 = new SimpleDataStructure ();
    VoxelDataStructure dataStructure4 = new SimpleDataStructure ();
    int step = 1;

    FastNoise noise = new FastNoise ();

    MarchingCubesGPU mcGPU = new MarchingCubesGPU ();
    MarchingCubes mc = new MarchingCubes ();
    MarchingCubesEnhanced transvoxel = new MarchingCubesEnhanced ();
    VoxelChunk chunk32, chunk16, chunk8, chunk4;
    TestVoxelWorld world32, world16, world8, world4;
    Voxel[] voxels1D = new Voxel[16 * 16 * 16];
    Voxel[, , ] voxels3D = new Voxel[16, 16, 16];
    Voxel[][][] voxelsJagged = new Voxel[16][][];

    List<VoxelChunk> affectedChunks1;
    List<VoxelChunk> affectedChunks2;

    BrushTool brushTool = new BrushTool ();
    SmoothTool smoothTool = new SmoothTool ();
    FlattenTool flattenTool = new FlattenTool ();

    [OneTimeSetUp]
    public void Init () {
        chunk32 = MakeChunk (chunkSize32, dataStructure32, Vector3Int.zero);
        chunk16 = MakeChunk (chunkSize16, dataStructure16, Vector3Int.zero);
        chunk8 = MakeChunk (chunkSize8, dataStructure8, Vector3Int.zero);
        chunk4 = MakeChunk (chunkSize4, dataStructure4, Vector3Int.zero);

        world32 = MakeWorld (chunkSize32, chunk32);
        world16 = MakeWorld (chunkSize16, chunk16);
        world8 = MakeWorld (chunkSize8, chunk8);
        world4 = MakeWorld (chunkSize4, chunk4);

        for (int x = 0; x < voxelsJagged.Length; x++) {
            voxelsJagged[x] = new Voxel[16][];
            for (int y = 0; y < voxelsJagged.Length; y++) {
                voxelsJagged[x][y] = new Voxel[16];
            }
        }
        affectedChunks1 = new List<VoxelChunk> ();
        affectedChunks2 = new List<VoxelChunk> ();

        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++) {
                    affectedChunks1.Add (MakeChunk (chunkSize16, new SimpleDataStructure (), new Vector3Int (x, y, z)));
                }

        for (int x = -2; x <= 2; x++)
            for (int y = -2; y <= 2; y++)
                for (int z = -2; z <= 2; z++) {
                    affectedChunks2.Add (MakeChunk (chunkSize16, new SimpleDataStructure (), new Vector3Int (x, y, z)));
                }
    }

    [Test]
    public void MarchingCubesGPUTest32 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            mcGPU.GenerateMesh (chunk32.voxels.ToArray (), chunk32.size, chunk32.voxelScale, step);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void MarchingCubesGPUTest16 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            mcGPU.GenerateMesh (chunk16.voxels.ToArray (), chunk16.size, chunk16.voxelScale, step);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void MarchingCubesGPUTest8 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            mcGPU.GenerateMesh (chunk8.voxels.ToArray (), chunk8.size, chunk8.voxelScale, step);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void MarchingCubesGPUTest4 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            mcGPU.GenerateMesh (chunk4.voxels.ToArray (), chunk4.size, chunk4.voxelScale, step);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void MarchingCubesCPUTest32 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            mc.GenerateMesh (chunk32.voxels.ToArray (), chunk32.size, chunk32.voxelScale, step);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void MarchingCubesCPUTest16 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            mc.GenerateMesh (chunk16.voxels.ToArray (), chunk16.size, chunk16.voxelScale, step);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void MarchingCubesCPUTest8 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            mc.GenerateMesh (chunk8.voxels.ToArray (), chunk8.size, chunk8.voxelScale, step);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void MarchingCubesCPUTest4 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            mc.GenerateMesh (chunk4.voxels.ToArray (), chunk4.size, chunk4.voxelScale, step);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void TransvoxelCPUTest32 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            transvoxel.GenerateMesh (world32, Vector3Int.zero, 1, 1f, chunkSize32.x);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void TransvoxelCPUTest16 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            transvoxel.GenerateMesh (world16, Vector3Int.zero, 1, 1f, chunkSize16.x);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void TransvoxelCPUTest8 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            transvoxel.GenerateMesh (world8, Vector3Int.zero, 1, 1f, chunkSize8.x);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void TransvoxelCPUTest4 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            transvoxel.GenerateMesh (world4, Vector3Int.zero, 1, 1f, chunkSize4.x);
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void OneDGet () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            for (int j = 0; j < voxels1D.Length; j++) {
                Voxel voxel = voxels1D[j];
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void OneDSet () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            for (int j = 0; j < voxels1D.Length; j++) {
                voxels1D[j] = new Voxel { };
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void ThreeDGet () {
        Vector3Int size = new Vector3Int (voxels3D.GetLength (0), voxels3D.GetLength (1), voxels3D.GetLength (2));
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    for (int z = 0; z < size.z; z++) {
                        Voxel voxel = voxels3D[x, y, z];
                    }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void ThreeDSet () {
        Vector3Int size = new Vector3Int (voxels3D.GetLength (0), voxels3D.GetLength (1), voxels3D.GetLength (2));
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    for (int z = 0; z < size.z; z++) {
                        voxels3D[x, y, z] = new Voxel { };
                    }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void JaggedGet () {
        Stopwatch watch = new Stopwatch ();
        for (int i = 0; i < cycles; i++) {
            for (int x = 0; x < voxelsJagged.Length; x++)
                for (int y = 0; y < voxelsJagged.Length; y++)
                    for (int z = 0; z < voxelsJagged.Length; z++) {
                        Voxel voxel = voxelsJagged[x][y][z];
                    }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void JaggedSet () {
        Stopwatch watch = new Stopwatch ();
        for (int i = 0; i < cycles; i++) {
            for (int x = 0; x < voxelsJagged.Length; x++) {
                for (int y = 0; y < voxelsJagged.Length; y++) {
                    for (int z = 0; z < voxelsJagged.Length; z++) {
                        voxelsJagged[x][y][z] = new Voxel { };
                    }
                }
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void UtilMap3DTo1DTest () {
        Vector3Int size = new Vector3Int (voxels3D.GetLength (0), voxels3D.GetLength (1), voxels3D.GetLength (2));
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    for (int z = 0; z < size.z; z++) {
                        int index = Util.Map3DTo1D (new Vector3Int (x, y, z), size);
                        Voxel voxel = voxels1D[index];
                    }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void UtilMap1DTo3DTest () {
        Vector3Int size = new Vector3Int (voxels3D.GetLength (0), voxels3D.GetLength (1), voxels3D.GetLength (2));
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            for (int j = 0; j < voxels3D.Length; j++) {
                Vector3Int coord = Util.Map1DTo3D (j, size);
                Voxel voxel = voxels3D[coord.x, coord.y, coord.z];
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void Map3DTo1DTest () {
        Vector3Int size = new Vector3Int (16, 16, 16);
        int sizeX = size.x;
        int sizeY = size.y;
        int sizeZ = size.z;
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++) {
                        int index = Map3DTo1D (x, y, z, sizeX, sizeY, sizeZ);
                        Voxel voxel = voxels1D[index];
                    }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void Map1DTo3DTest () {
        Vector3Int size = new Vector3Int (16, 16, 16);
        int sizeX = size.x;
        int sizeY = size.y;
        int sizeZ = size.z;
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            for (int j = 0; j < voxels3D.Length; j++) {
                Vector3Int coord = Map1DTo3D (j, sizeX, sizeY, sizeZ);
                Voxel voxel = voxels3D[coord.x, coord.y, coord.z];
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void BrushToolTest4 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks1) {
                brushTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 4, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void BrushToolTest8 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks1) {
                brushTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 8, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void BrushToolTest12 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks2) {
                brushTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 12, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void BrushToolTest16 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks2) {
                brushTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 16, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void SmoothToolTest4 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks1) {
                smoothTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 4, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void SmoothToolTest8 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks1) {
                smoothTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 8, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void SmoothToolTest12 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks2) {
                smoothTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 12, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void SmoothToolTest16 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks2) {
                smoothTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 16, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void FlattenToolTest4 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks2) {
                flattenTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 4, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void FlattenToolTest8 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks2) {
                flattenTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 8, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void FlattenToolTest12 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks2) {
                flattenTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 12, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    [Test]
    public void FlattenToolTest16 () {
        Stopwatch watch = new Stopwatch ();
        watch.Start ();
        for (int i = 0; i < cycles; i++) {
            foreach (var chunk in affectedChunks2) {
                flattenTool.ToolDrag (chunk, Vector3.zero, Vector3.zero, 0.04f, 16, 0.5f, world16);
            }
        }
        watch.Stop ();
        UnityEngine.Debug.Log (watch.ElapsedMilliseconds);
    }

    private VoxelChunk MakeChunk (Vector3Int size, VoxelDataStructure dataStructure, Vector3Int chunkCoord) {
        Voxel[] voxels = new Voxel[size.x * size.y * size.z];

        for (int i = 0; i < voxels.Length; i++) {
            Vector3Int coord = Util.Map1DTo3D (i, size);
            float density = noise.GetPerlinFractal (coord.x * 4, coord.y * 4, coord.z * 4);
            voxels[i] = new Voxel { density = density };
        }

        dataStructure.SetVoxels (voxels);
        var chunk = new VoxelChunk (chunkCoord, size, voxelScale, dataStructure);
        return chunk;

    }

    private TestVoxelWorld MakeWorld (Vector3Int size, VoxelChunk chunk) {
        TestVoxelWorld world = new TestVoxelWorld ();
        world.chunkSize = size.x;
        for (int x = -3; x <= 3; x++)
            for (int y = -3; y <= 3; y++)
                for (int z = -3; z <= 3; z++) {
                    world.chunkDictionary.Add (new Vector3Int (x, y, z), chunk);
                }
        return world;
    }

    public static int Map3DTo1D (int x, int y, int z, int size) {
        return x + y * size + z * size * size;
    }

    public static int Map3DTo1D (int x, int y, int z, int sizeX, int sizeY, int sizeZ) {
        return (sizeX * sizeY * z) + (sizeX * y) + x;
    }

    public static Vector3Int Map1DTo3D (int i, int size) {
        var x = i % size;
        var y = (i / size) % size;
        var z = i / (size * size);
        return new Vector3Int (x, y, z);
    }

    public static Vector3Int Map1DTo3D (int i, int sizeX, int sizeY, int sizeZ) {
        var x = i % sizeX;
        var y = (i / sizeX) % sizeY;
        var z = i / (sizeX * sizeY);
        return new Vector3Int (x, y, z);
    }

}