using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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

    [MenuItem ("Window/Voxelizer")]
    public static void Init () {
        var window = GetWindow<Voxelizer2> ("Voxelizer", true);
        window.Show ();
    }

    public void Awake () { }

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

            //find average triangle area
            float averageTriangleArea = 0f;
            for (int t = 0; t < mesh.triangles.Length; t += 3) {
                GeoMath.Triangle triangle = new GeoMath.Triangle (
                    mesh.vertices[mesh.triangles[t]],
                    mesh.vertices[mesh.triangles[t + 1]],
                    mesh.vertices[mesh.triangles[t + 2]]
                );
                // var cross = (Vector3.Cross (
                //     new Vector3 (Mathf.Abs (triangle.AB.direction.x), Mathf.Abs (triangle.AB.direction.y), Mathf.Abs (triangle.AB.direction.z)),
                //     new Vector3 (Mathf.Abs (triangle.AC.direction.x), Mathf.Abs (triangle.AC.direction.y), Mathf.Abs (triangle.AC.direction.z))
                // ));
                // Vector3 area = 0.5f * new Vector3 (Mathf.Abs (cross.x), Mathf.Abs (cross.y), Mathf.Abs (cross.z));

                Vector3 V = Vector3.Cross (triangle.a - triangle.b, triangle.a - triangle.c);
                float area = V.magnitude * 0.5f;
                averageTriangleArea += area;
            }
            int triNumber = mesh.triangles.Length / 3;
            averageTriangleArea /= triNumber;
            resolution = new Vector3Int ((int) (averageTriangleArea * triNumber), (int) (averageTriangleArea * triNumber), (int) (averageTriangleArea * triNumber));

            Vector3 voxelScale = new Vector3 (
                modelBoundingBox.size.x / resolution.x,
                modelBoundingBox.size.y / resolution.y,
                modelBoundingBox.size.z / resolution.z
            );
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

                // Vector3[] weightedNormals = new Vector3[7];
                // weightedNormals[0] = triangle.SurfaceNormal ();

                // 

                // for (int j = 0; j < mesh.triangles.Length; j += 3) {
                //     GeoMath.Triangle incident = new GeoMath.Triangle (
                //         mesh.vertices[mesh.triangles[j]],
                //         mesh.vertices[mesh.triangles[j + 1]],
                //         mesh.vertices[mesh.triangles[j + 2]]
                //     );

                //     Vector3 incidentNormal = incident.SurfaceNormal ();

                //     // A B C
                //     if ((triangle.a == incident.a || triangle.a == incident.b || triangle.a == incident.c)) weightedNormals[4] += GetVertexNormal (triangle.a, incident, incidentNormal);
                //     if ((triangle.b == incident.a || triangle.b == incident.b || triangle.b == incident.c)) weightedNormals[5] += GetVertexNormal (triangle.b, incident, incidentNormal);
                //     if ((triangle.c == incident.a || triangle.c == incident.b || triangle.c == incident.c)) weightedNormals[6] += GetVertexNormal (triangle.c, incident, incidentNormal);

                //     // if (incident.a == triangle.a) weightedNormals[4] += Mathf.Abs (Vector3.Angle (incident.AB.direction, incident.AC.direction)) * incidentNormal;
                //     // if (incident.b == triangle.b) weightedNormals[5] += Mathf.Abs (Vector3.Angle (incident.BA.direction, incident.BC.direction)) * incidentNormal;
                //     // if (incident.c == triangle.c) weightedNormals[6] += Mathf.Abs (Vector3.Angle (incident.CA.direction, incident.CB.direction)) * incidentNormal;

                //     // AB BC CA
                //     if (triangle.a == incident.a && triangle.b == incident.b && triangle.c == incident.c) continue;
                //     if (
                //         (triangle.a == incident.a || triangle.a == incident.b || triangle.a == incident.c) &&
                //         (triangle.b == incident.a || triangle.b == incident.b || triangle.b == incident.c)
                //     ) { weightedNormals[1] = Mathf.PI * triNormal + Mathf.PI * incidentNormal; }

                //     if (
                //         (triangle.b == incident.a || triangle.b == incident.b || triangle.b == incident.c) &&
                //         (triangle.c == incident.a || triangle.c == incident.b || triangle.c == incident.c)
                //     ) { weightedNormals[2] = Mathf.PI * triNormal + Mathf.PI * incidentNormal; }

                //     if (
                //         (triangle.a == incident.a || triangle.a == incident.b || triangle.a == incident.c) &&
                //         (triangle.c == incident.a || triangle.c == incident.b || triangle.c == incident.c)
                //     ) { weightedNormals[3] = Mathf.PI * triNormal + Mathf.PI * incidentNormal; }

                //     // if (incident.a == triangle.a && incident.b == triangle.b)
                //     //     if (incident.b == triangle.b && incident.c == triangle.c) weightedNormals[2] = Mathf.PI * triNormal + Mathf.PI * incidentNormal;
                //     // if (incident.a == triangle.a && incident.c == triangle.c) weightedNormals[3] = Mathf.PI * triNormal + Mathf.PI * incidentNormal;

                // }
                // weightedNormals[1].Normalize ();
                // weightedNormals[2].Normalize ();
                // weightedNormals[3].Normalize ();
                // weightedNormals[4].Normalize ();
                // weightedNormals[5].Normalize ();
                // weightedNormals[6].Normalize ();
                VoxelizeTriangle (voxels, distances, triangle, t, modelBoundingBox, voxelScale, vertexNormals, edgeNormals);
            }

            var mc = new MC ();
            var meshData = mc.GenerateMesh (voxels, isoLevel, resolution, voxelScale);
            var voxelMesh = meshData.BuildMesh ();

            previewVoxelMesh = EditorUtility.CreateGameObjectWithHideFlags ("Preview", HideFlags.HideAndDontSave, new System.Type[] {
                typeof (MeshFilter),
                typeof (MeshRenderer)
            });
            previewVoxelMesh.GetComponent<MeshFilter> ().sharedMesh = voxelMesh;
            previewVoxelMesh.GetComponent<MeshRenderer> ().material = asset.GetComponentInChildren<MeshRenderer> ().sharedMaterial;

        }
    }

    private Vector3 GetVertexNormal (Vector3 triVertex, GeoMath.Triangle incident, Vector3 incidentNormal) {
        Vector3 dir0, dir1;
        if (triVertex == incident.a) {
            dir0 = incident.AB.direction;
            dir1 = incident.AC.direction;
        } else if (triVertex == incident.b) {
            dir0 = incident.BA.direction;
            dir1 = incident.BC.direction;
        } else {
            dir0 = incident.CA.direction;
            dir1 = incident.CB.direction;
        }

        return (Mathf.Abs (Vector3.Angle (dir0, dir1)) * incidentNormal).normalized;
    }

    private void VoxelizeTriangle (Voxel[] voxels, float[] distances, GeoMath.Triangle triangle, int triangleIndex, Bounds modelBoundingBox, Vector3 voxelScale, Vector3[] vertexNormals, Vector3[] edgeNormals) {
        Bounds triangleBoundingBox = TriangleBoundingBox (triangle);
        Vector3 triangleCenter = triangle.Center ();
        triangleBoundingBox.Expand (voxelScale.x);
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

        Plane plane = new Plane (triangle.a, triangle.b, triangle.c);
        var lowerResCoord = MapSpaces (modelBoundingBox.min, modelBoundingBox.max, Vector3.zero, new Vector3Int (resolution.x - 1, resolution.y - 1, resolution.z - 1), triangleBoundingBox.min);
        var upperResCoord = MapSpaces (modelBoundingBox.min, modelBoundingBox.max, Vector3.zero, new Vector3Int (resolution.x - 1, resolution.y - 1, resolution.z - 1), triangleBoundingBox.max);

        for (int x = Mathf.CeilToInt (lowerResCoord.x); x <= Mathf.FloorToInt (upperResCoord.x); x++)
            for (int y = Mathf.CeilToInt (lowerResCoord.y); y <= Mathf.FloorToInt (upperResCoord.y); y++)
                for (int z = Mathf.CeilToInt (lowerResCoord.z); z <= Mathf.FloorToInt (upperResCoord.z); z++) {
                    Vector3Int resPos = new Vector3Int (x, y, z);
                    var modelCoords = MapSpaces (Vector3Int.zero, new Vector3Int (resolution.x - 1, resolution.y - 1, resolution.z - 1), triangleBoundingBox.min, triangleBoundingBox.max, resPos);
                    // if (!triangleBoundingBox.Contains (modelCoords) || !modelBoundingBox.Contains (modelCoords)) continue;

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
                    float prevSignDistance = distances[Util.Map3DTo1D (resPos, resolution)];

                    Vector3 regionNormal = Vector3.zero;
                    if (region == 0) regionNormal = triangle.SurfaceNormal ();
                    else if (region <= 3) regionNormal = edgeNormals[triangleIndex + (region - 1)];
                    else regionNormal = vertexNormals[triangleIndex + (region - 4)];

                    float density = Vector3.Dot (regionNormal, directionVector);
                    float prevDensity = voxels[Util.Map3DTo1D (resPos, resolution)].density;

                    if (signDistance < prevSignDistance) voxels[Util.Map3DTo1D (resPos, resolution)] = new Voxel { density = density };
                    else if (signDistance == prevSignDistance) voxels[Util.Map3DTo1D (resPos, resolution)] = new Voxel { density = prevDensity + density };
                }

        // for (var z = triangleBoundingBox.min.z; z <= triangleBoundingBox.max.z; z += voxelScale.z)
        //     for (var y = triangleBoundingBox.min.y; y <= triangleBoundingBox.max.y; y += voxelScale.y)
        //         for (var x = triangleBoundingBox.min.x; x <= triangleBoundingBox.max.x; x += voxelScale.x) {
        //             if (!modelBoundingBox.Contains (new Vector3 (x, y, z))) continue;
        //             Vector3 closestPoint = plane.ClosestPointOnPlane (new Vector3 (x, y, z));
        //             // x = closest point on triangle
        //             // p = given point p
        //             // r = p - x
        //             Vector3 directionVector = new Vector3 (x, y, z) - closestPoint;

        //             float minDistance = float.MaxValue;
        //             int region = 0;
        //             for (int i = 0; i < regions.Length; i++) {
        //                 float distance = Vector3.Distance (regions[i], new Vector3 (x, y, z));
        //                 if (distance < minDistance) {
        //                     minDistance = distance;
        //                     region = i;
        //                 }
        //             }
        //             float signDistance = GetSignDistance (region, closestPoint, triangle, plane);
        //             var weightedNormal = weightedNormals[region];

        //             float density = Vector3.Dot (weightedNormal, directionVector);
        //             Vector3Int resolutionCoord = MapModelSpaceToResolution (new Vector3 (x, y, z), modelBoundingBox);
        //             // Debug.Log (resolutionCoord);
        //             Voxel v = voxels[Util.Map3DTo1D (resolutionCoord, resolution)];
        //             float prevSignDistance = distances[Util.Map3DTo1D (resolutionCoord, resolution)];

        //             if (signDistance < prevSignDistance) {
        //                 density = Vector3.Dot (weightedNormal, directionVector);
        //                 distances[Util.Map3DTo1D (resolutionCoord, resolution)] = signDistance;
        //             } else if (signDistance == prevSignDistance) density = v.density + Vector3.Dot (weightedNormal, directionVector);

        //             voxels[Util.Map3DTo1D (resolutionCoord, resolution)] = new Voxel { density = density };

        //         }
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

    private Vector3Int MapModelSpaceToResolution (Vector3 pos, Bounds modelBoundingBox) {
        return new Vector3Int (
            (int) (pos.x / modelBoundingBox.size.x * (resolution.x - 1)),
            (int) (pos.y / modelBoundingBox.size.y * (resolution.y - 1)),
            (int) (pos.z / modelBoundingBox.size.z * (resolution.z - 1)));
    }

    private Vector3 MapSpaces (Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, Vector3 s) {
        var v1 = s - a1;
        var v2 = b2 - b1;
        var v3 = new Vector3 (v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        var v4 = a2 - a1;
        var v5 = new Vector3 (v3.x / v4.x, v3.y / v4.y, v3.z / v4.z);
        return new Vector3 (b1.x + v5.x, b1.y + v5.y, b1.z + v5.z);
    }

    private Bounds TriangleBoundingBox (GeoMath.Triangle triangle) {
        Bounds bounds = new Bounds ();
        var minX = Mathf.Min (Mathf.Min (triangle.a.x, triangle.b.x), triangle.c.x);
        var minY = Mathf.Min (Mathf.Min (triangle.a.y, triangle.b.y), triangle.c.y);
        var minZ = Mathf.Min (Mathf.Min (triangle.a.z, triangle.b.z), triangle.c.z);
        var maxX = Mathf.Max (Mathf.Max (triangle.a.x, triangle.b.x), triangle.c.x);
        var maxY = Mathf.Max (Mathf.Max (triangle.a.y, triangle.b.y), triangle.c.y);
        var maxZ = Mathf.Max (Mathf.Max (triangle.a.z, triangle.b.z), triangle.c.z);
        bounds.SetMinMax (new Vector3 (minX, minY, minZ), new Vector3 (maxX, maxY, maxZ));
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