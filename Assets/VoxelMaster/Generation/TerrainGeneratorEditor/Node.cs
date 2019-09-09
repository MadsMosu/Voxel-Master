using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Node : ScriptableObject
{

    public string nodeName;

    public Rect rect = new Rect(0, 0, 200, 140);

    public NodeInput[] inputs;
    public NodeOutput[] outputs;
    public abstract void ProcessNode();

}




public enum Type
{
    ANY,
    INT,
    FLOAT,
    VECTOR2,
    VECTOR3
}

public struct NodeInput
{
    public string name;
    public Type type;
    public NodeConnection connection;
    public Rect port;

}

public struct NodeOutput
{
    public string name;
    public dynamic value;
    public Rect port;
}

public struct NodeConnection
{
    public Node node;
    public int outputIndex;
}