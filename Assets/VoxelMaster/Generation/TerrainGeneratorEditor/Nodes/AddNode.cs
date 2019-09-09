using System;
using UnityEngine;

[Serializable]
public class AddNode : Node
{
    public AddNode()
    {
        nodeName = "Addition";
        inputs = new NodeInput[]{
            new NodeInput{
                name="A",
                type=Type.ANY,
            },
            new NodeInput{
                name="B",
                type=Type.ANY,
            },
            new NodeInput{
                name="C",
                type=Type.ANY,
            },
        };

        outputs = new NodeOutput[]{
            new NodeOutput{
                name= "Result",
            },
            new NodeOutput{
                name= "Result",
            }
        };
    }

    public override void ProcessNode()
    {
        var a = inputs[0].connection.node.outputs[inputs[0].connection.outputIndex].value;
        var b = inputs[1].connection.node.outputs[inputs[1].connection.outputIndex].value;
        if (!CheckType(a) && !CheckType(b))
        {
            try
            {
                outputs[0].value = a + b;
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }

    bool CheckType(dynamic obj)
    {
        return obj.GetType() is int || obj.GetType() is float;
    }

}