

using UnityEngine;

public struct Voxel
{

    private float density;
    public float Density
    {
        get { return density; }
        set { density = Mathf.Clamp01(value); }
    }

    public int type;
}