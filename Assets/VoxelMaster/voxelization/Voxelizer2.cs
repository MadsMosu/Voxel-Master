using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VoxelMaster.Chunk;

//http://www2.imm.dtu.dk/pubdb/views/edoc_download.php/1289/pdf/imm1289.pdf

public class Voxelizer2 : EditorWindow {
    private static float isoLevel = 0f;
    private static float filterWidth = (2 * Mathf.Sqrt (3));
    public GameObject asset, previewVoxelMesh;
    PreviewRenderUtility previewRenderer;
    Editor gameObjectEditor;
    private List<Vector3> vertices = new List<Vector3> ();
    private List<int> indices = new List<int> ();
    private Vector3Int resolution;
    private Editor meshPreviewEditor;
    private VoxelChunk chunk;
    private float voxelScale;

    [MenuItem ("Window/Voxelizer")]
    public static void Init () {
        var window = GetWindow<Voxelizer2> ("Voxelizer", true);
        window.Show ();
    }

    public void OnGUI () {
        asset = (GameObject) EditorGUILayout.ObjectField ("GameObject:", asset, typeof (GameObject), false);
        if (previewVoxelMesh != null) {
            if (meshPreviewEditor == null) {
                meshPreviewEditor = Editor.CreateEditor (previewVoxelMesh);
            }
            meshPreviewEditor.OnInteractivePreviewGUI (GUILayoutUtility.GetRect (500, 500), EditorStyles.whiteLabel);
        }

        if (GUILayout.Button ("Voxelize") && asset != null) {

            MeshFilter meshFilter = asset.GetComponentInChildren<MeshFilter> ();
            Mesh mesh = TranslatedMesh (asset, meshFilter);
            Bounds modelBoundingBox = mesh.bounds;
            // Debug.Log (modelBoundingBox);

            // BoundsInt voxelSpaceBound = new BoundsInt (
            //     Mathf.FloorToInt (modelBoundingBox.min.x / voxelScale), Mathf.FloorToInt (modelBoundingBox.min.y / voxelScale), Mathf.FloorToInt (modelBoundingBox.min.z / voxelScale),
            //     Mathf.CeilToInt (modelBoundingBox.size.x / voxelScale), Mathf.CeilToInt (modelBoundingBox.size.y / voxelScale), Mathf.CeilToInt (modelBoundingBox.size.z / voxelScale)
            // );
            // Debug.Log (voxelSpaceBound);

            //find average triangle area
            float averageTriangleArea = 0f;
            for (int t = 0; t < mesh.triangles.Length; t += 3) {
                GeoMath.Triangle triangle = new GeoMath.Triangle (
                    mesh.vertices[mesh.triangles[t]],
                    mesh.vertices[mesh.triangles[t + 1]],
                    mesh.vertices[mesh.triangles[t + 2]]
                );

                Vector3 V = Vector3.Cross (triangle.a - triangle.b, triangle.a - triangle.c);
                float area = V.magnitude * 0.5f;
                averageTriangleArea += area;
            }
            int triNumber = mesh.triangles.Length / 3;
            averageTriangleArea /= triNumber;
            voxelScale = 1f;
            resolution = new Vector3Int (Mathf.CeilToInt (modelBoundingBox.max.x), Mathf.CeilToInt (modelBoundingBox.max.y), Mathf.CeilToInt (modelBoundingBox.max.z));
            // Debug.Log (resolution);

            // voxelScale = ((voxelSpaceBound.size.x + voxelSpaceBound.size.y + voxelSpaceBound.size.z) / 3) / ((resolution.x + resolution.y + resolution.z) / 3);

            Voxel[] voxels = new Voxel[resolution.x * resolution.y * resolution.z];
            float[] distances = new float[resolution.x * resolution.y * resolution.z];
            for (int i = 0; i < distances.Length; i++) {
                distances[i] = Mathf.Infinity;
            }

            Vector3[] vertexNormals = new Vector3[mesh.triangles.Length];
            Vector3[] edgeNormals = new Vector3[mesh.triangles.Length];

            for (int t = 0; t < mesh.triangles.Length; t += 3) {
                GeoMath.Triangle triangle = new GeoMath.Triangle (
                    mesh.vertices[mesh.triangles[t]],
                    mesh.vertices[mesh.triangles[t + 1]],
                    mesh.vertices[mesh.triangles[t + 2]]
                );

                Vector3 triNormal = triangle.SurfaceNormal ();
                vertexNormals[t] = (Mathf.Abs (Vector3.Angle (triangle.AB.direction, triangle.AC.direction)) * triNormal).normalized;
                vertexNormals[t + 1] = (Mathf.Abs (Vector3.Angle (triangle.BA.direction, triangle.BC.direction)) * triNormal).normalized;
                vertexNormals[t + 2] = (Mathf.Abs (Vector3.Angle (triangle.CA.direction, triangle.CB.direction)) * triNormal).normalized;

                Vector3 edgeNormal = (Mathf.PI * triNormal).normalized;
                edgeNormals[t] = edgeNormal;
                edgeNormals[t + 1] = edgeNormal;
                edgeNormals[t + 2] = edgeNormal;

                VoxelizeTriangle (voxels, distances, triangle, t, modelBoundingBox, vertexNormals, edgeNormals);
            }

            var mc = new MarchingCubesGPU ();
            chunk = new VoxelChunk (Vector3Int.zero, resolution, voxelScale, new SimpleDataStructure ());
            var meshData = mc.GenerateMesh (chunk);
            var voxelMesh = meshData.BuildMesh ();

            previewVoxelMesh = EditorUtility.CreateGameObjectWithHideFlags ("Preview", HideFlags.HideAndDontSave, new System.Type[] {
                typeof (MeshFilter),
                typeof (MeshRenderer)
            });
            previewVoxelMesh.GetComponent<MeshFilter> ().sharedMesh = voxelMesh;
            previewVoxelMesh.GetComponent<MeshRenderer> ().material = asset.GetComponentInChildren<MeshRenderer> ().sharedMaterial;

        }
    }

    private void VoxelizeTriangle (Voxel[] voxels, float[] distances, GeoMath.Triangle triangle, int triangleIndex, Bounds modelBoundingBox, Vector3[] vertexNormals, Vector3[] edgeNormals) {
        Bounds triangleBoundingBox = TriangleBoundingBox (triangle);
        Vector3 triangleCenter = triangle.Center ();
        triangleBoundingBox.Expand (voxelScale);

        Plane plane = new Plane (triangle.a, triangle.b, triangle.c);
        Vector3[] regions = new Vector3[] {
            triangleCenter,
            triangleCenter + (GetSpPoint (triangle.a, triangle.b, triangleCenter) - triangleCenter) * 2,
            triangleCenter + (GetSpPoint (triangle.b, triangle.c, triangleCenter) - triangleCenter) * 2,
            triangleCenter + (GetSpPoint (triangle.c, triangle.a, triangleCenter) - triangleCenter) * 2,
            triangleCenter + (triangle.a - triangleCenter) * 2,
            triangleCenter + (triangle.b - triangleCenter) * 2,
            triangleCenter + (triangle.c - triangleCenter) * 2
        };
        // var r1 = triangleCenter;
        // var r2 = triangleCenter + (GetSpPoint (triangle.a, triangle.b, triangleCenter) - triangleCenter) * 2;
        // var r3 = triangleCenter + (GetSpPoint (triangle.b, triangle.c, triangleCenter) - triangleCenter) * 2;
        // var r4 = triangleCenter + (GetSpPoint (triangle.c, triangle.a, triangleCenter) - triangleCenter) * 2;
        // var r5 = triangleCenter + (((r2 + r4) / 2 - r1).normalized * (Vector3.Distance (r1, triangle.a) * 2));
        // var r6 = triangleCenter + (((r2 + r3) / 2 - r1).normalized * (Vector3.Distance (r1, triangle.b) * 2));
        // var r7 = triangleCenter + (((r3 + r4) / 2 - r1).normalized * (Vector3.Distance (r1, triangle.c) * 2));
        // Vector3[] regions = new Vector3[] { r1, r2, r3, r4, r5, r6, r7 };

        for (float x = triangleBoundingBox.min.x; x <= triangleBoundingBox.max.x; x += voxelScale)
            for (float y = triangleBoundingBox.min.y; y <= triangleBoundingBox.max.y; y += voxelScale)
                for (float z = triangleBoundingBox.min.z; z <= triangleBoundingBox.max.z; z += voxelScale) {
                    Vector3 resPos = new Vector3 (x, y, z);
                    Vector3Int voxelPos = new Vector3Int (Mathf.FloorToInt (x / voxelScale), Mathf.FloorToInt (y / voxelScale), Mathf.FloorToInt (z / voxelScale));
                    if (!triangleBoundingBox.Contains (resPos) || !modelBoundingBox.Contains (resPos)) continue;

                    Vector3 closestPoint = plane.ClosestPointOnPlane (new Vector3 (x, y, z));
                    Vector3 directionVector = new Vector3 (x, y, z) - closestPoint;

                    float minDistance = float.MaxValue;
                    int region = 0;
                    for (int i = 0; i < regions.Length; i++) {
                        float distance = Vector3.Distance (regions[i], new Vector3 (x, y, z));
                        if (distance < minDistance) {
                            minDistance = distance;
                            region = i;
                        }
                    }
                    float signDistance = GetSignDistance (region, closestPoint, triangle, plane);
                    float prevSignDistance = distances[Util.Map3DTo1D (voxelPos, resolution)];

                    Vector3 regionNormal = Vector3.zero;
                    if (region == 0) regionNormal = triangle.SurfaceNormal ();
                    else if (region <= 3) regionNormal = edgeNormals[triangleIndex + (region - 1)];
                    else regionNormal = vertexNormals[triangleIndex + (region - 4)];
                    Debug.Log (region);
                    float density = -Vector3.Dot (regionNormal, directionVector);
                    float prevDensity = voxels[Util.Map3DTo1D (voxelPos, resolution)].density;

                    if (signDistance < prevSignDistance) voxels[Util.Map3DTo1D (voxelPos, resolution)] = new Voxel { density = density };
                    else if (signDistance == prevSignDistance) voxels[Util.Map3DTo1D (voxelPos, resolution)] = new Voxel { density = prevDensity + density };
                }
    }

    private float GetSignDistance (int region, Vector3 closestPoint, GeoMath.Triangle triangle, Plane plane) {
        switch (region) {
            case 0:
                return Mathf.Abs (plane.GetDistanceToPoint (closestPoint));
            case 1:
                return Mathf.Abs (Vector3.Distance (NearestPointOnLine (triangle.AB, closestPoint), closestPoint));
            case 2:
                return Mathf.Abs (Vector3.Distance (NearestPointOnLine (triangle.BC, closestPoint), closestPoint));
            case 3:
                return Mathf.Abs (Vector3.Distance (NearestPointOnLine (triangle.AC, closestPoint), closestPoint));
            case 4:
                return Mathf.Abs (Vector3.Distance (triangle.a, closestPoint));
            case 5:
                return Mathf.Abs (Vector3.Distance (triangle.b, closestPoint));
            case 6:
                return Mathf.Abs (Vector3.Distance (triangle.c, closestPoint));
            default:
                return 0f;
        }
    }

    private static Vector3 NearestPointOnLine (GeoMath.Line3D line, Vector3 closestPoint) {
        float d = Vector3.Dot (line.direction, closestPoint - line.p1) / line.direction.sqrMagnitude;
        d = Mathf.Clamp01 (d);
        return Vector3.Lerp (line.p1, line.p2, d);
    }

    private Bounds TriangleBoundingBox (GeoMath.Triangle triangle) {
        Bounds bounds = new Bounds (triangle.a, Vector3.zero);
        bounds.Encapsulate (triangle.b);
        bounds.Encapsulate (triangle.c);
        return bounds;
    }

    private Vector3 GetSpPoint (Vector3 a, Vector3 b, Vector3 c) {
        var x1 = a.x;
        var y1 = a.y;
        var z1 = a.z;
        var x2 = b.x;
        var y2 = b.y;
        var z2 = b.z;
        var x3 = c.x;
        var y3 = c.y;
        var z3 = c.z;

        var px = x2 - x1;
        var py = y2 - y1;
        var pz = z2 - z1;

        var dAB = px * px + py * py + pz * pz;
        var u = ((x3 - x1) * px + (y3 - y1) * py + (z3 - z1) * pz) / dAB;
        var x = x1 + u * px;
        var y = y1 + u * py;
        var z = z1 + u * pz;
        return new Vector3 (x, y, z);
    }

    private Mesh TranslatedMesh (GameObject asset, MeshFilter meshFilter) {
        Bounds boundingBox = new Bounds (asset.transform.position, Vector3.zero);
        meshFilter.sharedMesh.RecalculateBounds ();
        boundingBox.Encapsulate (meshFilter.sharedMesh.bounds);

        //move all vertices to positive coords
        Vector3 boundingBoxMin = boundingBox.min;
        boundingBox = new Bounds ();

        Mesh mesh = new Mesh ();
        Vector3[] vertices = new Vector3[meshFilter.sharedMesh.vertices.Length];
        for (int v = 0; v < meshFilter.sharedMesh.vertices.Length; v++) {
            vertices[v] = meshFilter.sharedMesh.vertices[v] -= boundingBoxMin;
        }
        mesh.vertices = vertices;
        mesh.triangles = meshFilter.sharedMesh.triangles;
        mesh.normals = meshFilter.sharedMesh.normals;

        mesh.RecalculateBounds ();
        boundingBox.Encapsulate (mesh.bounds);

        return mesh;

    }

    private void SaveVoxelizedMesh (VoxelizedMesh voxelMesh, string path, string name) {
        AssetDatabase.CreateAsset (voxelMesh, path + "/" + name + ".asset");
        AssetDatabase.SaveAssets ();
        EditorUtility.FocusProjectWindow ();
        Selection.activeObject = voxelMesh;
    }

    public void OnDestroy () { }

}