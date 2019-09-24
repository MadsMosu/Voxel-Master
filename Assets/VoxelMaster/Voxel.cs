using System;
using UnityEngine;
public class Voxel
{
    private float density = 0f;
    public float Density {
        get {return density; }
        set {density = Mathf.Clamp01(value); }
    }
}