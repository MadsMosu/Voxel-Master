// using UnityEngine;
// using VoxelMaster.Chunk;

// public class SmoothTool : VoxelTool {
//     public override string name => "Smooth Terrain";
//     float[] gaussianKernel;

//     public override void OnToolGUI () { }
//     public override void ToolStart (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
//         gaussianKernel = new float[125];
//         float sumTotal = 0;
//         float sd = 2f;
//         float euler = 1.0f / (2.0f * Mathf.PI * Mathf.Pow (10, 2));

//         //kernel generation
//         for (int x = -2; x <= 2; x++)
//             for (int y = -2; y <= 2; y++)
//                 for (int z = -2; z <= 2; z++) {
//                     float distance = ((y * y) + (x * x) + (z * z)) / (2 * sd * sd);
//                     Vector3Int kernelCoord = new Vector3Int (x + 2, y + 2, z + 2);
//                     float value = euler * Mathf.Exp (-distance);
//                     int index = Util.Map3DTo1D (kernelCoord, 5);
//                     gaussianKernel[index] = value;
//                     sumTotal += value;
//                 }

//         //normalize kernel
//         for (int x = 0; x < 6; x++)
//             for (int y = 0; y < 6; y++)
//                 for (int z = 0; z < 6; z++) {
//                     Vector3Int kernelCoord = new Vector3Int (x, y, z);
//                     int index = Util.Map3DTo1D (kernelCoord, 5);
//                     gaussianKernel[index] *= (1.0f / sumTotal);
//                 }
//     }

//     public override void ToolDrag (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
//         Vector3Int chunkWorldPosition = chunk.coords * (chunk.size - Vector3Int.one);
//         Voxel[] smoothedVoxels = new Voxel[chunk.voxels.ToArray ().Length];

//         chunk.voxels.Traverse ((x, y, z, v) => {
//             Vector3Int voxelCoord = new Vector3Int (x, y, z);
//             Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
//             if (
//                 (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
//                 (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
//             ) {
//                 float density = GaussianSmooth (chunk, voxelCoord);
//                 v.density = density;
//                 // smoothedVoxels[Util.Map3DTo1D (voxelCoord, chunk.size)] = new Voxel (density, v.materialIndex);
//                 smoothedVoxels[Util.Map3DTo1D (voxelCoord, chunk.size)] = v;
//                 // Debug.Log (v.density);

//             }
//         });
//         // chunk.voxels.SetVoxels (smoothedVoxels);
//         chunk.voxels.Traverse ((x, y, z, v) => {
//             Vector3Int voxelCoord = new Vector3Int (x, y, z);
//             Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
//             if (
//                 (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
//                 (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
//             ) {
//                 int index = Util.Map3DTo1D (voxelCoord, chunk.size);
//                 // Debug.Log (smoothedVoxels[index].density);
//                 if (Mathf.Abs (smoothedVoxels[index].density - v.density) < 0.3f) return;
//                 v.density = Mathf.MoveTowards (v.density, smoothedVoxels[index].density, intensity);
//                 chunk.voxels.SetVoxel (index, v);
//             }
//         });

//         // chunk.voxels.Traverse ((x, y, z, v) => {
//         //     Vector3Int voxelCoord = new Vector3Int (x, y, z);
//         //     Vector3Int voxelWorldPosition = chunkWorldPosition + voxelCoord;
//         //     //if within radius
//         //     if (
//         //         (voxelWorldPosition.x <= position.x + radius && voxelWorldPosition.y <= position.y + radius && voxelWorldPosition.z <= position.z + radius) &&
//         //         (voxelWorldPosition.x >= position.x - radius && voxelWorldPosition.y >= position.y - radius && voxelWorldPosition.z >= position.z - radius)
//         //     ) {
//         //         float tempIntensity = intensity;
//         //         if (falloff > 0) {
//         //             float scaleFactor = Vector3.Distance (voxelWorldPosition, position) * falloff;
//         //             tempIntensity /= scaleFactor;
//         //         }
//         //         float avgDensity = avgDensities[Util.Map3DTo1D (voxelCoord, chunk.size)];
//         //         if (Mathf.Abs (avgDensity - v.density) < 0.30f) return;
//         //         else v.density = Mathf.MoveTowards (v.density, avgDensity, tempIntensity);

//         //         chunk.voxels.SetVoxel (voxelCoord, v);
//         //     }
//         // });
//     }

//     public override void ToolEnd (VoxelChunk chunk, Vector3 position, Vector3 surfaceNormal, float intensity, int radius, float falloff) {
//         // Do nothing
//     }

//     private float GaussianSmooth (VoxelChunk chunk, Vector3Int voxelCoord) {
//         int i = 0;
//         float sumDensity = 0f;
//         for (int x = -2; x <= 2; x++)
//             for (int y = -2; y <= 2; y++)
//                 for (int z = -2; z <= 2; z++) {
//                     if (
//                         (voxelCoord.x + x < 0 || voxelCoord.x + x >= chunk.size.x) ||
//                         (voxelCoord.y + y < 0 || voxelCoord.y + y >= chunk.size.y) ||
//                         (voxelCoord.z + z < 0 || voxelCoord.z + z >= chunk.size.z)
//                     ) continue;
//                     Vector3Int neighborCoord = new Vector3Int (x, y, z);
//                     int neighborIndex = Util.Map3DTo1D (neighborCoord + Vector3Int.one + Vector3Int.one, 5);
//                     // Debug.Log (chunk.voxels.GetVoxel (voxelCoord + neighborCoord).density);
//                     sumDensity += chunk.voxels.GetVoxel (voxelCoord + neighborCoord).density * gaussianKernel[neighborIndex];
//                     i++;
//                 }
//         return sumDensity / i;
//     }
// }