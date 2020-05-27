// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Runtime.InteropServices;
// using System.Runtime.Serialization.Formatters.Binary;
// using UnityEngine;

// [System.Serializable]
// public class SimpleDataStructure : VoxelDataStructure {
//     private Vector3Int size;
//     private Voxel[] voxels;

//     public override void Init (Vector3Int size) {
//         this.size = size;
//         this.voxels = new Voxel[size.x * size.y * size.z];
//     }

//     public override Voxel GetVoxel (Vector3Int coords) {
//         return voxels[Util.Map3DTo1D (coords, size)];
//     }

//     public override Voxel GetVoxel (int index) {
//         return voxels[index];
//     }

//     public override void SetVoxel (Vector3Int coords, Voxel voxel) {
//         voxels[Util.Map3DTo1D (coords, size)] = voxel;
//     }

//     public override void SetVoxel (int index, Voxel voxel) {
//         voxels[index] = voxel;
//     }

//     public override void Traverse (Action<int, int, int, Voxel> function) {

//         int sizeX = size.x;
//         int sizeY = size.y;
//         int sizeZ = size.z;

//         for (int i = 0; i < voxels.Length; i++) {
//             var coord = Util.Map1DTo3D (i, size.x, size.x, size.z);
//             function.Invoke (coord.x, coord.y, coord.z, voxels[i]);
//         }
//     }
//     public override void TraverseZYX (Action<int, int, int, Voxel> function) {
//         for (int z = 0; z < size.x; z++)
//             for (int y = 0; y < size.y; y++)
//                 for (int x = 0; x < size.z; x++) {
//                     function.Invoke (x, y, z, voxels[Util.Map3DTo1D (new Vector3Int (x, y, z), size)]);
//                 }

//     }

//     public async override void Save (BufferedStream stream) {

//         var header = new FileHeader {
//             chunkSize = size
//         };

//         var formatter = new BinaryFormatter ();
//         formatter.Serialize (stream, header);

//     }

//     public override Voxel[][][] ToArray () {
//         return new Voxel[0][][];
//     }

//     public override void SetVoxels (Voxel[] voxels) {
//         this.voxels = voxels;
//     }

//     public override Voxel[] ExtractRegion (BoundsInt bound, Dictionary<Vector3Int, List<int>> labels, int labelFilter) {
//         Voxel[] region = new Voxel[(bound.size.x + 1) * (bound.size.y + 1) * (bound.size.z + 1)];
//         for (int x = bound.min.x; x <= bound.max.x; x++)
//             for (int y = bound.min.y; y <= bound.max.y; y++)
//                 for (int z = bound.min.z; z <= bound.max.z; z++) {
//                     Vector3Int coords = new Vector3Int (x, y, z);
//                     int index = Util.Map3DTo1D (coords, size);

//                     Voxel voxel;
//                     if (((x <= bound.min.x || y <= bound.min.y || z <= bound.min.z) || (x >= bound.max.x || y >= bound.max.y || z >= bound.max.z)) ||
//                         ((x <= 0 || y <= 0 || z <= 0) || (x >= size.x || y >= size.y || z >= size.z)) ||
//                         (voxels[index].density >.0f && labels[coords].All (label => label != labelFilter))) {
//                         voxel = new Voxel { density = -1 };
//                     } else {
//                         voxel = voxels[index];
//                     }
//                     region[Util.Map3DTo1D (coords - bound.min, bound.size + Vector3Int.one)] = voxel;
//                     staticVoxels.Add (coords);
//                 }
//         return region;
//     }
// }