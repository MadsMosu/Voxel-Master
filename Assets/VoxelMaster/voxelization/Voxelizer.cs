// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEngine;

// public class Voxelizer : EditorWindow {
//     private static float isoLevel = 0.5f;
//     private static float filterWidth = (2 * Mathf.Sqrt (3));
//     public GameObject asset, previewVoxelMesh;
//     PreviewRenderUtility previewRenderer;
//     Editor gameObjectEditor;
//     private List<Vector3> vertices = new List<Vector3> ();
//     private List<int> indices = new List<int> ();
//     private Vector3Int resolution;
//     private Editor meshPreviewEditor;

//     [MenuItem ("Window/Voxelizer")]
//     public static void Init () {
//         var window = GetWindow<Voxelizer> ("Voxelizer", true);
//         window.Show ();
//     }

//     public void Awake () { }

//     public void OnGUI () {
//         asset = (GameObject) EditorGUILayout.ObjectField ("GameObject:", asset, typeof (GameObject), false);
//         resolution = EditorGUILayout.Vector3IntField ("Resolution:", resolution);
//         if (previewVoxelMesh != null) {
//             if (meshPreviewEditor == null) {
//                 meshPreviewEditor = Editor.CreateEditor (previewVoxelMesh);
//             }
//             meshPreviewEditor.OnInteractivePreviewGUI (GUILayoutUtility.GetRect (500, 500), EditorStyles.whiteLabel);
//         }

//         if (GUILayout.Button ("Voxelize") && asset != null && resolution != Vector3Int.zero) {

//             MeshFilter[] meshFilters = asset.GetComponentsInChildren<MeshFilter> ();
//             Bounds modelBoundingBox = CalculateLocalBounds (asset, meshFilters);
//             Vector3 voxelScale = new Vector3 (
//                 modelBoundingBox.size.x / resolution.x,
//                 modelBoundingBox.size.y / resolution.y,
//                 modelBoundingBox.size.z / resolution.z
//             );
//             Voxel[] voxels = new Voxel[resolution.x * resolution.y * resolution.z];

//             //triangle bounding box: take min x,y,z and max x,y,z
//             for (int i = 0; i < meshFilters.Length; i++) {
//                 if (meshFilters[i].sharedMesh) {

//                     // MeshRenderer meshRenderer = meshFilters[i].gameObject.GetComponent<MeshRenderer> ();
//                     Mesh mesh = meshFilters[i].sharedMesh;
//                     // for (int j = 0; j < meshFilters[i].sharedMesh.subMeshCount; j++) {
//                     for (int t = 0; t < mesh.triangles.Length; t += 3) {
//                         MathToolBox.Triangle triangle = new MathToolBox.Triangle (
//                             mesh.vertices[mesh.triangles[t]],
//                             mesh.vertices[mesh.triangles[t + 1]],
//                             mesh.vertices[mesh.triangles[t + 2]]
//                         );
//                         // Debug.Log ($"A: {triangle.a} B: {triangle.b} C: {triangle.c}");
//                         VoxelizeTriangle (voxels, triangle, modelBoundingBox, voxelScale);
//                     }
//                     // }
//                 }
//             }
//             var mc = new MC ();
//             var meshData = mc.GenerateMesh (voxels, isoLevel, resolution, voxelScale);
//             var voxelMesh = meshData.BuildMesh ();

//             previewVoxelMesh = EditorUtility.CreateGameObjectWithHideFlags ("Preview", HideFlags.HideAndDontSave, new System.Type[] {
//                 typeof (MeshFilter),
//                 typeof (MeshRenderer)
//             });
//             previewVoxelMesh.GetComponent<MeshFilter> ().sharedMesh = voxelMesh;
//             // previewVoxelMesh.GetComponent<MeshRenderer> ().material = new Material (Shader.Find ("Default-Diffuse.mat"));

//         }
//     }

//     private void VoxelizeTriangle (Voxel[] voxels, MathToolBox.Triangle triangle, Bounds modelBoundingBox, Vector3 voxelScale) {
//         MathToolBox.Plane3D a = new MathToolBox.Plane3D (triangle.a, triangle.b, triangle.c);
//         a.Normalize (a.normal.magnitude);

//         MathToolBox.Plane3D b = new MathToolBox.Plane3D (triangle.AC.direction, triangle.a);
//         b.Normalize (b.normal.magnitude * -triangle.AC.length);
//         MathToolBox.Plane3D c = new MathToolBox.Plane3D (triangle.CB.direction, triangle.c);
//         c.Normalize (c.normal.magnitude * -triangle.CB.length);
//         MathToolBox.Plane3D d = new MathToolBox.Plane3D (triangle.BA.direction, triangle.b);
//         d.Normalize (d.normal.magnitude * -triangle.BA.length);

//         MathToolBox.Plane3D e = new MathToolBox.Plane3D (Vector3.Cross (a.normal, triangle.AC.direction), triangle.AC.Center ());
//         e.FlipNormal ();
//         e.Normalize (e.normal.magnitude);
//         MathToolBox.Plane3D f = new MathToolBox.Plane3D (Vector3.Cross (a.normal, triangle.CB.direction), triangle.CB.Center ());
//         f.FlipNormal ();
//         f.Normalize (f.normal.magnitude);
//         MathToolBox.Plane3D g = new MathToolBox.Plane3D (Vector3.Cross (a.normal, triangle.BA.direction), triangle.BA.Center ());
//         g.FlipNormal ();
//         g.Normalize (g.normal.magnitude);
//         TriangleRegions regions = new TriangleRegions (a, b, c, d, e, f, g);

//         //plane a normal is perpendiculear to any vector along the edges of the triangle vertices
//         Debug.Assert (Vector3.Dot (a.normal, triangle.a - triangle.b) == 0);
//         Debug.Assert (Vector3.Dot (a.normal, triangle.b - triangle.c) == 0);
//         Debug.Assert (Vector3.Dot (a.normal, triangle.a - triangle.c) == 0);
//         Debug.Assert (a.normal.magnitude == 1);
//         Debug.Assert (e.normal.magnitude == 1);
//         Debug.Assert (f.normal.magnitude == 1);
//         Debug.Assert (g.normal.magnitude == 1);

//         Bounds triangleBoundingBox = TriangleBoundingBox (triangle);
//         Bounds S = new Bounds (triangleBoundingBox.center, new Vector3 (
//             triangleBoundingBox.max.x + (filterWidth * voxelScale.x),
//             triangleBoundingBox.max.y + (filterWidth * voxelScale.y),
//             triangleBoundingBox.max.z + (filterWidth * voxelScale.z)
//         ));
//         MathToolBox.Plane3D filter = new MathToolBox.Plane3D (Vector3.Cross (a.normal, (triangle.Center () + a.normal) - (triangle.Center () - a.normal)), triangle.Center ());
//         filter.Normalize (filter.normal.magnitude);

//         Debug.Assert (Vector3.Dot (filter.normal, a.normal) == 0);
//         Debug.Log (filter.normal);

//         var dist = filter.normal.x * triangleBoundingBox.min.x + filter.normal.y * triangleBoundingBox.min.y + filter.normal.z * triangleBoundingBox.min.z + filter.DistanceFromOrigin ();
//         var xStep = filter.normal.x;
//         var yStep = filter.normal.y - filter.normal.x * triangleBoundingBox.max.x;
//         var zStep = filter.normal.z - filter.normal.y * triangleBoundingBox.max.y - filter.normal.x * triangleBoundingBox.max.x;
//         for (var z = triangleBoundingBox.min.z; z <= triangleBoundingBox.max.z; z += voxelScale.z) {
//             for (var y = triangleBoundingBox.min.y; y <= triangleBoundingBox.max.y; y += voxelScale.y) {
//                 for (var x = triangleBoundingBox.min.x; x <= triangleBoundingBox.max.x; x += voxelScale.x) {

//                     float density = GetVoxelDensity (new Vector3 (x, y, z), regions, triangle, S, dist);
//                     if (density != 0f) {
//                         Vector3Int resolutionCoord = MapModelSpaceToResolution (new Vector3 (x, y, z), modelBoundingBox);
//                         Voxel v = voxels[Util.Map3DTo1D (resolutionCoord, resolution)];
//                         // Debug.Log (density);
//                         if (v.density != 0f) density = Mathf.Max (density, v.density);
//                         voxels[Util.Map3DTo1D (resolutionCoord, resolution)] = new Voxel { density = density };
//                     }
//                     dist += xStep;
//                 }
//                 dist += yStep;
//             }
//             dist += zStep;
//         }
//     }

//     private Vector3Int MapModelSpaceToResolution (Vector3 pos, Bounds modelBoundingBox) {
//         return new Vector3Int (
//             (int) (pos.x / modelBoundingBox.size.x * (resolution.x - 1)),
//             (int) (pos.y / modelBoundingBox.size.y * (resolution.y - 1)),
//             (int) (pos.z / modelBoundingBox.size.z * (resolution.z - 1)));
//     }

//     private float GetVoxelDensity (Vector3 voxelPos, TriangleRegions regions, MathToolBox.Triangle triangle, Bounds S, float dist) {
//         var aDist = regions.a.DistanceToPoint (voxelPos);
//         var bDist = regions.b.DistanceToPoint (voxelPos);
//         var cDist = regions.c.DistanceToPoint (voxelPos);
//         var dDist = regions.d.DistanceToPoint (voxelPos);
//         var eDist = regions.e.DistanceToPoint (voxelPos);
//         var fDist = regions.f.DistanceToPoint (voxelPos);
//         var gDist = regions.g.DistanceToPoint (voxelPos);

//         bool intersectA = (aDist > -S.max.x && aDist > -S.max.y && aDist > -S.max.z) && (aDist < S.max.x && aDist < S.max.y && aDist < S.max.z);

//         if (intersectA) {
//             if (eDist >= 0 && fDist >= 0 && gDist >= 0) return R1Density (aDist); // R1
//             else if (dDist >= 0 && dDist <= 1 && gDist < 0) return R234Density (aDist, gDist); // R2
//             else if (cDist >= 0 && cDist <= 1 && fDist < 0) return R234Density (aDist, fDist); // R3
//             else if (bDist >= 0 && bDist <= 1 && eDist < 0) return R234Density (aDist, eDist); // R4
//             else if (bDist < 0 && dDist > 1) return R567Density (triangle.a, voxelPos); // R5
//             else if (cDist > 1 && dDist < 0) return R567Density (triangle.b, voxelPos); // R6
//             else if (bDist > 1 && cDist < 0) return R567Density (triangle.c, voxelPos); // R7
//             else return 0;
//         } else return 0;

//     }

//     private Bounds TriangleBoundingBox (MathToolBox.Triangle triangle) {
//         Bounds bounds = new Bounds ();
//         var minX = Mathf.Min (Mathf.Min (triangle.a.x, triangle.b.x), triangle.c.x);
//         var minY = Mathf.Min (Mathf.Min (triangle.a.y, triangle.b.y), triangle.c.y);
//         var minZ = Mathf.Min (Mathf.Min (triangle.a.z, triangle.b.z), triangle.c.z);
//         var maxX = Mathf.Max (Mathf.Max (triangle.a.x, triangle.b.x), triangle.c.x);
//         var maxY = Mathf.Max (Mathf.Max (triangle.a.y, triangle.b.y), triangle.c.y);
//         var maxZ = Mathf.Max (Mathf.Max (triangle.a.z, triangle.b.z), triangle.c.z);
//         bounds.SetMinMax (new Vector3 (minX, minY, minZ), new Vector3 (maxX, maxY, maxZ));
//         return bounds;
//     }

//     private Bounds CalculateLocalBounds (GameObject asset, MeshFilter[] meshFilters) {
//         Quaternion currentRotation = asset.transform.rotation;
//         asset.transform.rotation = Quaternion.Euler (0f, 0f, 0f);
//         Bounds boundingBox = new Bounds (asset.transform.position, Vector3.zero);

//         for (int i = 0; i < meshFilters.Length; i++) {
//             if (meshFilters[i].sharedMesh) {
//                 MeshRenderer meshRenderer = meshFilters[i].gameObject.GetComponent<MeshRenderer> ();
//                 boundingBox.Encapsulate (meshFilters[i].sharedMesh.bounds);
//             }
//         }
//         Vector3 localCenter = boundingBox.center - asset.transform.position;
//         boundingBox.center = localCenter;
//         asset.transform.rotation = currentRotation;

//         //move bounds and all vertices to positive coords
//         if ((boundingBox.min.x < 0 || boundingBox.min.y < 0 || boundingBox.min.z < 0)) {
//             boundingBox.center = boundingBox.extents;
//             for (int i = 0; i < meshFilters.Length; i++) {
//                 if (meshFilters[i].sharedMesh.vertices.Any (v => v.x < 0 || v.y < 0 || v.z < 0)) {
//                     if (meshFilters[i].sharedMesh) {
//                         Mesh mesh = meshFilters[i].sharedMesh;
//                         Vector3[] vertices = new Vector3[mesh.vertices.Length];
//                         for (int v = 0; v < mesh.vertices.Length; v++) {
//                             vertices[v] = mesh.vertices[v] += boundingBox.extents;
//                         }
//                         mesh.vertices = vertices;

//                     }
//                 }
//             }
//         }
//         return boundingBox;
//     }

//     private float R1Density (float aDist) {
//         return 1 - (Mathf.Abs (aDist) / filterWidth);
//     }

//     private float R234Density (float aDist, float bDist) {
//         return 1 - ((Mathf.Sqrt (Mathf.Pow (aDist, 2) + Mathf.Pow (bDist, 2))) / filterWidth);
//     }

//     private float R567Density (Vector3 triangleVertex, Vector3 origin) {
//         return 1 - ((Mathf.Sqrt (Mathf.Pow (triangleVertex.x - origin.x, 2) + Mathf.Pow (triangleVertex.y - origin.y, 2) + Mathf.Pow (triangleVertex.z - origin.z, 2))) / filterWidth);
//     }

//     private struct TriangleRegions {
//         public MathToolBox.Plane3D a, b, c, d, e, f, g;
//         public TriangleRegions (MathToolBox.Plane3D a, MathToolBox.Plane3D b, MathToolBox.Plane3D c, MathToolBox.Plane3D d, MathToolBox.Plane3D e, MathToolBox.Plane3D f, MathToolBox.Plane3D g) {
//             this.a = a;
//             this.b = b;
//             this.c = c;
//             this.d = d;
//             this.e = e;
//             this.f = f;
//             this.g = g;
//         }
//     }

//     private void SaveVoxelizedMesh (VoxelizedMesh voxelMesh, string path, string name) {
//         AssetDatabase.CreateAsset (voxelMesh, path + "/" + name + ".asset");
//         AssetDatabase.SaveAssets ();
//         EditorUtility.FocusProjectWindow ();
//         Selection.activeObject = voxelMesh;
//     }

//     public void OnDestroy () { }

// }