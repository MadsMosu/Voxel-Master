using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Node : ScriptableObject
{

    public string nodeName;

    public NodeInput[] inputs;
    public NodeOutput[] outputs;

    public Vector2 position;

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
    public NodeOutput connection;
}

public struct NodeOutput
{
    public string name;
    public dynamic value;
}