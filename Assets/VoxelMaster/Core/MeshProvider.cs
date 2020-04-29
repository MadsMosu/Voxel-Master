using UnityEngine;

public class MeshProvider {

    private MarchingCubesEnhancedGPU MCGPU;

    public MeshProvider (MeshGeneratorSettings settings) {
        MCGPU = new MarchingCubesEnhancedGPU (settings);
    }

    public MeshData GetMesh (IVoxelData volume, Vector3Int origin, int step) {
        return MCGPU.GenerateMesh (volume, origin, step);
    }
}