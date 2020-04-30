using System;
using UnityEngine;

public class VoxelObject : MonoBehaviour, IVoxelData {
    private float voxelScale;
    public int chunkSize, isoLevel, chunkNumb;
    public ISignedDistanceField distanceField;
    public Vector3 size;
    private Mesh mesh;
    GameObject go;

    public Voxel this [Vector3 v] {
        get => this [(int) v.x, (int) v.y, (int) v.z];
        set => this [(int) v.x, (int) v.y, (int) v.z] = value;
    }
    public Voxel this [Vector3Int v] {
        get => GetVoxel (v);
        set => SetVoxel (v, value);
    }

    private void SetVoxel (Vector3Int v, Voxel value) {
        throw new NotImplementedException ();
    }

    private Voxel GetVoxel (Vector3Int v) {
        throw new NotImplementedException ();
    }

    public Voxel this [int x, int y, int z] {
        get => this [new Vector3Int (x, y, z)];
        set => this [new Vector3Int (x, y, z)] = value;
    }

    void OnEnable () {
        voxelScale = ((size.x + size.y + size.z) / 3) / (chunkNumb * chunkSize);

        MeshGeneratorSettings meshSettings = new MeshGeneratorSettings {
            chunkSize = chunkSize,
            voxelScale = voxelScale,
            isoLevel = isoLevel
        };

        MarchingCubesEnhanced MC = new MarchingCubesEnhanced ();
        MC.Init (meshSettings);
        // mesh = MC.GenerateMesh(this, go.tran)
    }

    void Start () {
        go = CreateGameObject ();
    }

    void Update () {

    }

    private void Decompose () {

    }

    private GameObject CreateGameObject () {
        GameObject go = new GameObject ();
        VoxelObject voxelObject = go.AddComponent<VoxelObject> ();
        MeshFilter meshFilter = go.AddComponent<MeshFilter> ();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer> ();
        MeshCollider meshCollider = go.AddComponent<MeshCollider> ();
        go.AddComponent<Rigidbody> ();
        return go;
    }

    private float SignedDistanceSphere (Vector3 pos, Vector3 center, float radius) {
        return Vector3.Distance (pos, center) - radius;
    }

}