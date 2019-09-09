using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class PerlinNoise3D : Node
{

    public bool isFractal;
    [Input] public Vector3 inputPosition;
    [Output] public float result;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        var pos = GetInputValue<Vector3>("inputPosition", inputPosition) ;
        if (isFractal)
        {
            return TerrainGraph.FastNoise.GetPerlinFractal(pos.x, pos.y, pos.z);
        }
        else
        {
            return TerrainGraph.FastNoise.GetPerlin(pos.x, pos.y, pos.z);
        }
    }
}