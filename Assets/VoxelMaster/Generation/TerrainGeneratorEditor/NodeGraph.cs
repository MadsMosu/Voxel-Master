using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VoxelMaster/Terrain Generator")]
public class NodeGraph : ScriptableObject
{
    // [HideInInspector]
    public List<Node> nodes = new List<Node>();

    void OnEnable()
    {
        AddTestNode();
    }

    void AddTestNode()
    {
        var node = ScriptableObject.CreateInstance<AddNode>();
        node.nodeName = "test";
        nodes.Add(node);
    }


}