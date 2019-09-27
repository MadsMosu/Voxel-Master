using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using XNode;

[CreateAssetMenu]
public class TerrainGraph : NodeGraph
{
    public static FastNoise FastNoise = new FastNoise();

    public TerrainGraph()
    {
    }

    private Output outputNode;
    public float Evaluate(Vector3 pos)
    {
        if (outputNode == null)
            outputNode = nodes.Find(x => x is Output) as Output;

        Thread.SetData(Thread.GetNamedDataSlot("voxelPosition"), pos);
        return outputNode.GetInputValue<float>("value", 0);
    }
}