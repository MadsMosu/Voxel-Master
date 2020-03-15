using UnityEngine;

public interface IVoxelData {
    Voxel this [int x, int y, int z] { get; set; }
    Voxel this [Vector3Int v] { get; set; }
    Voxel this [Vector3 v] { get; set; }
}