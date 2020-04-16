using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Voxelizer : EditorWindow {
    private static float isovalue = 0.5f;
    private Vector3 voxelScale;
    private float filterWidth = (2 * Mathf.Sqrt (3));
    public GameObject asset;
    PreviewRenderUtility previewRenderer;
    Editor gameObjectEditor;
    private List<Vector3> vertices = new List<Vector3> ();
    private List<int> indices = new List<int> ();
    private Vector3Int resolution;
    private Bounds boundingBox;
    private Voxel[] voxels;

    [MenuItem ("Window/Voxelizer")]
    public static void Init () {
        var window = GetWindow<Voxelizer> ("Voxelizer", true);
        window.Show ();
    }

    public void Awake () { }

    public void OnGUI () {
        asset = (GameObject) EditorGUILayout.ObjectField ("GameObject", asset, typeof (GameObject), false);
        resolution = EditorGUILayout.Vector3IntField ("Resolution: ", resolution);

        if (asset != null && resolution != Vector3Int.zero && voxels == null) {
            if (gameObjectEditor == null) {
                gameObjectEditor = Editor.CreateEditor (asset);
            }
            voxels = new Voxel[resolution.x * resolution.y * resolution.z];
            // gameObjectEditor.OnInteractivePreviewGUI (GUILayoutUtility.GetRect (500, 500), EditorStyles.whiteLabel);

            MeshFilter[] meshFilters = asset.GetComponentsInChildren<MeshFilter> ();
            CalculateLocalBounds (asset, meshFilters);
            voxelScale = new Vector3 (
                boundingBox.size.x / resolution.x,
                boundingBox.size.y / resolution.y,
                boundingBox.size.z / resolution.z
            );

            //triangle bounding box: take min x,y,z and max x,y,z
            for (int i = 0; i < meshFilters.Length; i++) {
                if (meshFilters[i].sharedMesh) {

                    // MeshRenderer meshRenderer = meshFilters[i].gameObject.GetComponent<MeshRenderer> ();
                    Mesh mesh = meshFilters[i].sharedMesh;
                    // for (int j = 0; j < meshFilters[i].sharedMesh.subMeshCount; j++) {
                    for (int t = 0; t < mesh.triangles.Length; t += 3) {
                        Triangle triangle = new Triangle {
                        a = mesh.vertices[mesh.triangles[t]],
                        b = mesh.vertices[mesh.triangles[t + 1]],
                        c = mesh.vertices[mesh.triangles[t + 2]],
                        };
                        // Debug.Log ($"A: {triangle.a} B: {triangle.b} C: {triangle.c}");
                        VoxelizeTriangle (triangle);
                    }
                    // }
                }
            }
        }
    }

    private void VoxelizeTriangle (Triangle triangle) {
        Plane a = new Plane (triangle.a, triangle.b, triangle.c);
        Plane b = new Plane (triangle.c.normalized * (1.0f / Vector3.Distance (triangle.a, triangle.c)), triangle.a);
        Plane c = new Plane (triangle.b.normalized * (1.0f / Vector3.Distance (triangle.c, triangle.b)), triangle.c);
        Plane d = new Plane (triangle.a.normalized * (1.0f / Vector3.Distance (triangle.b, triangle.a)), triangle.b);
        Plane e = new Plane (Vector3.Cross (a.normal, triangle.a - triangle.b).normalized, (triangle.a + triangle.b) / 2);
        Plane f = new Plane (Vector3.Cross (a.normal, triangle.b - triangle.c).normalized, (triangle.b + triangle.c) / 2);
        Plane g = new Plane (Vector3.Cross (a.normal, triangle.c - triangle.a).normalized, (triangle.c + triangle.a) / 2);
        TriangleRegions regions = new TriangleRegions (a, b, c, d, e, f, g);

        Bounds triangleBoundingBox = TriangleBoundingBox (triangle);
        Bounds S = new Bounds (boundingBox.center, new Vector3 (
            triangleBoundingBox.max.x + (filterWidth * voxelScale.x),
            triangleBoundingBox.max.y + (filterWidth * voxelScale.y),
            triangleBoundingBox.max.z + (filterWidth * voxelScale.z)
        ));
        var dist = a.distance * triangleBoundingBox.min.x + b.distance * triangleBoundingBox.min.y + c.distance * triangleBoundingBox.min.z + d.distance;
        var xStep = a.distance;
        var yStep = b.distance - a.distance * triangleBoundingBox.max.x;
        var zStep = c.distance - b.distance * triangleBoundingBox.max.y - a.distance * triangleBoundingBox.max.x;
        for (var z = triangleBoundingBox.min.z; z <= triangleBoundingBox.max.z; z += voxelScale.z) {
            for (var y = triangleBoundingBox.min.y; y <= triangleBoundingBox.max.y; y += voxelScale.y) {
                for (var x = triangleBoundingBox.min.x; x <= triangleBoundingBox.max.x; x += voxelScale.x) {

                    float density = GetVoxelDensity (new Vector3 (x, y, z), regions, triangle, S);
                    if (density != 0f) {
                        Vector3Int resolutionCoord = MapModelSpaceToResolution (new Vector3 (x, y, z));
                        voxels[Util.Map3DTo1D (resolutionCoord, resolution)] = new Voxel { density = density };
                    }
                    dist += xStep;
                }
                dist += yStep;
            }
            dist += zStep;
        }
        Debug.Log ("hello");
    }

    private Vector3Int MapModelSpaceToResolution (Vector3 pos) {
        return new Vector3Int (
            (int) (pos.x / boundingBox.size.x * resolution.x),
            (int) (pos.y / boundingBox.size.y * resolution.y),
            (int) (pos.z / boundingBox.size.z * resolution.z)
        );

    }

    private float GetVoxelDensity (Vector3 voxelPos, TriangleRegions regions, Triangle triangle, Bounds S) {
        var aDist = regions.a.GetDistanceToPoint (voxelPos);
        var bDist = regions.b.GetDistanceToPoint (voxelPos);
        var cDist = regions.c.GetDistanceToPoint (voxelPos);
        var dDist = regions.d.GetDistanceToPoint (voxelPos);
        var eDist = regions.e.GetDistanceToPoint (voxelPos);
        var fDist = regions.f.GetDistanceToPoint (voxelPos);
        var gDist = regions.g.GetDistanceToPoint (voxelPos);

        bool intersectA = (aDist >= S.min.x && aDist >= S.min.y && aDist >= S.min.z) && (aDist <= S.max.x && aDist <= S.max.y && aDist <= S.max.z);

        if (intersectA) {
            if (eDist >= 0 && fDist >= 0 && gDist >= 0) return R1Density (regions.a); // R1
            else if (dDist >= 0 && dDist <= 1 && gDist <= 0) return R234Density (regions.a, regions.g); // R2
            else if (cDist >= 0 && cDist <= 1 && fDist <= 0) return R234Density (regions.a, regions.f); // R3
            else if (bDist >= 0 && bDist <= 1 && eDist <= 0) return R234Density (regions.a, regions.e); // R4
            else if (bDist <= 0 && dDist >= 1) return R567Density (triangle.a, voxelPos); // R5
            else if (cDist >= 1 && dDist <= 0) return R567Density (triangle.b, voxelPos); // R6
            else if (bDist >= 1 && cDist <= 0) return R567Density (triangle.c, voxelPos); // R7
            else return 0;
        } else return 0;

    }

    private Bounds TriangleBoundingBox (Triangle triangle) {
        Bounds bounds = new Bounds ();
        var minX = Mathf.Min (Mathf.Min (triangle.a.x, triangle.b.x), triangle.c.x);
        var minY = Mathf.Min (Mathf.Min (triangle.a.y, triangle.b.y), triangle.c.y);
        var minZ = Mathf.Min (Mathf.Min (triangle.a.z, triangle.b.z), triangle.c.z);
        var maxX = Mathf.Max (Mathf.Max (triangle.a.x, triangle.b.x), triangle.c.x);
        var maxY = Mathf.Max (Mathf.Max (triangle.a.y, triangle.b.y), triangle.c.y);
        var maxZ = Mathf.Max (Mathf.Max (triangle.a.z, triangle.b.z), triangle.c.z);
        bounds.SetMinMax (new Vector3 (minX, minY, minZ), new Vector3 (maxX, maxY, maxZ));
        // bounds.center = new Vector3 ((minX + maxX) / 2, (minY + maxX) / 2, (minZ + maxZ) / 2);
        return bounds;
    }

    private void CalculateLocalBounds (GameObject asset, MeshFilter[] meshFilters) {
        Quaternion currentRotation = asset.transform.rotation;
        asset.transform.rotation = Quaternion.Euler (0f, 0f, 0f);
        boundingBox = new Bounds (asset.transform.position, Vector3.zero);

        for (int i = 0; i < meshFilters.Length; i++) {
            if (meshFilters[i].sharedMesh) {
                MeshRenderer meshRenderer = meshFilters[i].gameObject.GetComponent<MeshRenderer> ();
                boundingBox.Encapsulate (meshRenderer.bounds);
            }
        }
        Vector3 localCenter = boundingBox.center - asset.transform.position;
        boundingBox.center = localCenter;
        asset.transform.rotation = currentRotation;

    }

    public void OnDestroy () { }

    private float R1Density (Plane a) {
        return 1 - (Mathf.Abs (a.distance) / filterWidth);
    }

    private float R234Density (Plane a, Plane b) {
        return 1 - ((Mathf.Sqrt (Mathf.Pow (a.distance, 2) + Mathf.Pow (b.distance, 2))) / filterWidth);
    }

    private float R567Density (Vector3 triangleVertex, Vector3 origin) {
        return 1 - ((Mathf.Sqrt (Mathf.Pow (triangleVertex.x - origin.x, 2) + Mathf.Pow (triangleVertex.y - origin.y, 2) + Mathf.Pow (triangleVertex.z - origin.z, 2))) / filterWidth);
    }

    private struct Triangle {
        public Vector3 a, b, c;
    }

    private struct TriangleRegions {
        public Plane a, b, c, d, e, f, g;
        public TriangleRegions (Plane a, Plane b, Plane c, Plane d, Plane e, Plane f, Plane g) {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.e = e;
            this.f = f;
            this.g = g;
            a.normal.Normalize ();
        }
    }

    private void SaveVoxelizedMesh (VoxelizedMesh voxelMesh, string path, string name) {
        AssetDatabase.CreateAsset (voxelMesh, path + "/" + name + ".asset");
        AssetDatabase.SaveAssets ();
        EditorUtility.FocusProjectWindow ();
        Selection.activeObject = voxelMesh;
    }

}