using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using XNode;

public class Ground : Node
{


    [Input] public Vector3 inputPosition;
    [Input] public float heightMap;
    public float level;

    [Output] public float density;

    public override object GetValue(NodePort port)
    {
        var pos = GetInputValue<Vector3>("inputPosition", inputPosition);
        return (pos.y <= level) ? 1f : 0f;
    }
}