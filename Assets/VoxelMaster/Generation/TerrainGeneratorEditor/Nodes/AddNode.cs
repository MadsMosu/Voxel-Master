using System;

[Serializable]
public class AddNode : Node
{

    public AddNode()
    {
        this.inputs = new NodeInput[]{
            new NodeInput{
                name="a",
                type=Type.ANY
            },
            new NodeInput{
                name="b",
                type=Type.ANY
            },
        };

        this.outputs = new NodeOutput[]{
            new NodeOutput{
                name= "Result",
            }
        };
    }

    public override void ProcessNode()
    {
        var a = inputs[0].connection.value;
        var b = inputs[1].connection.value;
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