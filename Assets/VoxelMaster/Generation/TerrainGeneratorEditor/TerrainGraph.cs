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

    public float Evaluate(Vector3 pos)
    {
        Thread.SetData(Thread.GetNamedDataSlot("voxelPosition"), pos);
        var output = nodes.Find(x => x is Output) as Output;
        return output.GetInputValue<float>("value", 0);
    }
}
