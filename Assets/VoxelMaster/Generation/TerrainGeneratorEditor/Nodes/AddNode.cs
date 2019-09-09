using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class AddNode : Node
{
    [Input] public float a;
    [Input] public float b;
    [Output] public float result;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "result") return GetInputValue<float>("a", a) + GetInputValue<float>("b", b);
        else return null;
    }
}
