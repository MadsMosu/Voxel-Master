using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using XNode;

public class VoxelPosition : Node
{
    [Output] public Vector3 pos;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port)
    {
        return Thread.GetData(Thread.GetNamedDataSlot("voxelPosition")) ?? Vector3.zero;
    }
}