using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VoxelMaster/Terrain Generator")]
public class NodeGraph : ScriptableObject
{
    // [HideInInspector]
    public List<Node> nodes = new List<Node>();

    void OnEnable()
    {
    }

    public void AddNode<T>(Vector2 position) where T : Node
    {
        var node = ScriptableObject.CreateInstance<T>();
        node.rect.position = position;
        // float height = 100;
        // if (node.inputs.Length > node.outputs.Length) {
        //     foreach (var input in node.inputs) {
        //         height += 25;
        //     }
        // } else {
        //     foreach (var output in node.outputs) {
        //         height += 25;
        //     }
        // }
        // node.rect.height = height;
        nodes.Add(node);
    }

    public void RemoveNode<T>(Node node) where T : Node
    {
        nodes.Remove(node);
    }



}