using UnityEngine;

[System.Serializable]
public struct Voxel {

    public float density;
    public byte materialIndex;

    public Voxel(float density, byte materialIndex) {
        this.density = Mathf.Clamp01(density);
        this.materialIndex = materialIndex;
    }

    public Voxel(float density) : this(density, 0) { }
}
