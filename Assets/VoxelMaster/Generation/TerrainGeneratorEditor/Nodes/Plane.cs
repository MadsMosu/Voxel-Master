using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using XNode;

public class Plane : Node
{
    [Input] public Vector3 inputPosition;
    [Input] public float height;
    [Output] public float density;

    public override object GetValue(NodePort port)
    {
        var pos = GetInputValue<Vector3>("inputPosition", inputPosition);
        return (pos.y <= height) ? 1f : 0f;
    }
}