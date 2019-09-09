using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class Output : Node
{

    [Input] public float value;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return GetInputValue<float>("value", value);
    }
}