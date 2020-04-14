using System;
using System.Collections.Generic;
using UnityEngine;

public class Octree {

    private static readonly Vector3Int[][] diagonalNeighborLocations = new Vector3Int[][] {
        new Vector3Int[4] { new Vector3Int (-1, 1, -1), new Vector3Int (-1, 1, 1), new Vector3Int (1, 1, 1), new Vector3Int (1, 1, -1) },
        new Vector3Int[4] { new Vector3Int (-1, 0, -1), new Vector3Int (-1, 0, 1), new Vector3Int (1, 0, 1), new Vector3Int (1, 0, -1) },
        new Vector3Int[4] { new Vector3Int (-1, -1, -1), new Vector3Int (-1, -1, 1), new Vector3Int (1, -1, 1), new Vector3Int (1, -1, -1) }
    };

    int leafSize;
    byte maxDepth;

    Dictionary<uint, OctreeNode> nodes = new Dictionary<uint, OctreeNode> ();

    public Octree (int leafSize, byte depth) {
        this.leafSize = leafSize;
        maxDepth = depth;

        var rootNode = new OctreeNode {
            locationCode = 0b1,
            childrenFlags = 0x0,
            bounds = new Bounds (Vector3.zero, Vector3.one * (leafSize << depth + 1)),
        };

        nodes.Add (0b1, rootNode);

        // for (int i = 0; i < 25000; i++) {
        //     var pos = new Vector3Int (
        //         (int) UnityEngine.Random.Range (rootNode.bounds.min.x, rootNode.bounds.max.x) / leafSize,
        //         (int) UnityEngine.Random.Range (rootNode.bounds.min.y, rootNode.bounds.max.y) / leafSize,
        //         (int) UnityEngine.Random.Range (rootNode.bounds.min.z, rootNode.bounds.max.z) / leafSize
        //     );
        //     AddChunk (pos, null);
        // }

    }

    private void AddNode (OctreeNode node) {
        if (nodes.ContainsKey (node.locationCode)) throw new Exception ("LocationCode already occupied");
        nodes.Add (node.locationCode, node);

    }

    public void AddChunk (Vector3Int coords, VoxelChunk chunk) {
        OctreeNode node = nodes[0b1];

        var chunkPosition = coords * leafSize + (Vector3.one * leafSize / 2);
        if (!node.bounds.Contains (chunkPosition)) return;

        var currentDepth = GetNodeDepth (node);
        while (currentDepth < maxDepth) {
            currentDepth = GetNodeDepth (node);

            byte childLocationCode = GetChildLocationCode (chunkPosition, node);

            bool hasChild = isBitSet (node.childrenFlags, childLocationCode);

            if (!hasChild) {
                var oneFourth = node.bounds.size.x / 4;
                var childOffset = new Vector3 (
                    (childLocationCode & 0b001) > 0 ? oneFourth : -oneFourth,
                    (childLocationCode & 0b100) > 0 ? oneFourth : -oneFourth,
                    (childLocationCode & 0b010) > 0 ? oneFourth : -oneFourth
                );
                var child = new OctreeNode {
                    locationCode = (node.locationCode << 3) | childLocationCode,
                    childrenFlags = 0b0,
                    bounds = new Bounds (node.bounds.center + childOffset, node.bounds.size / 2),
                    chunk = currentDepth == maxDepth ? chunk : null
                };
                AddNode (child);
                node.childrenFlags ^= (byte) (1 << childLocationCode);
                node = child;
            } else {
                node = nodes[(node.locationCode << 3) | childLocationCode];
            }
        }

    }

    private byte GetChildLocationCode (Vector3 pos, OctreeNode node) {
        byte locationCode = 0b000;
        if (pos.x > node.bounds.center.x) locationCode |= 0b001;
        if (pos.y > node.bounds.center.y) locationCode |= 0b100;
        if (pos.z > node.bounds.center.z) locationCode |= 0b010;
        return locationCode;
    }

    public uint GetNodeIndexAtCoord (Vector3Int coord) {
        // Debug.Log ($"Input coord: {coord}");
        OctreeNode currentNode = nodes[0b1];

        byte currentDepth = 0;

        while (currentDepth < maxDepth) {
            currentDepth = GetNodeDepth (currentNode);
            byte childLocationCode = GetChildLocationCode (coord * leafSize + (Vector3.one * leafSize / 2), currentNode);

            bool hasChild = isBitSet (currentNode.childrenFlags, childLocationCode);
            if (!hasChild) return 0;

            currentNode = nodes[(currentNode.locationCode << 3) | childLocationCode];
        }

        Debug.Assert (currentDepth == maxDepth);
        // Debug.Log ($"output bounds: {currentNode.bounds}");
        return currentNode.locationCode;
    }

    public OctreeNode GetNode (uint locationCode) {
        if (!nodes.ContainsKey (locationCode)) return null;
        return nodes[locationCode];

    }

    public void DrawLeafNodes () {
        DrawLeafNodes (nodes[0b1]);
        // DrawAll (nodes[0b1]);

    }

    private void DrawLeafNodes (OctreeNode node) {
        var depth = GetNodeDepth (node);
        if (GetNodeDepth (node) == maxDepth + 1) {
            return;
        }
        Gizmos.color = Color.HSVToRGB ((maxDepth * 1.0f) / (depth * 1.0f), 1, 1);
        Gizmos.DrawWireCube (node.bounds.center, node.bounds.size);
        UnityEditor.Handles.Label (node.bounds.center + Vector3.up * node.bounds.extents.y, depth.ToString ());

        for (int i = 0; i < 8; i++) {
            if (isBitSet (node.childrenFlags, i)) {
                uint locCodeChild = (node.locationCode << 3) | (uint) i;
                OctreeNode child = nodes[locCodeChild];
                DrawLeafNodes (child);
            }
        }
    }

    internal List<OctreeNode> GetLeafChildren (uint locationCode) {
        var node = nodes[locationCode];

        var chunks = new List<OctreeNode> ();

        if (GetNodeDepth (node) >= maxDepth + 1) {
            chunks.Add (node);
            return chunks;
        }

        for (int i = 0; i < 8; i++) {
            if (isBitSet (node.childrenFlags, i)) {
                uint locCodeChild = (node.locationCode << 3) | (uint) i;
                chunks.AddRange (GetLeafChildren (locCodeChild));
            }
        }
        return chunks;
    }

    public List<OctreeNode> GetChildren (uint locationCode) {
        var node = nodes[locationCode];

        var chunks = new List<OctreeNode> ();

        if (GetNodeDepth (node) >= maxDepth + 1) {
            return chunks;
        }

        for (int i = 0; i < 8; i++) {
            if (isBitSet (node.childrenFlags, i)) {
                uint locCodeChild = (node.locationCode << 3) | (uint) i;
                chunks.Add (GetNode (locCodeChild));
            }
        }
        return chunks;
    }

    public List<uint> GetChildLocations (uint locationCode) {
        var node = this.nodes[locationCode];
        var nodes = new List<uint> ();
        if (GetNodeDepth (node) >= maxDepth + 1)
            return nodes;

        for (int i = 0; i < 8; i++) {
            if (isBitSet (node.childrenFlags, i)) {
                uint locCodeChild = (node.locationCode << 3) | (uint) i;
                nodes.Add (locCodeChild);
            }
        }
        return nodes;
    }

    public List<OctreeNode> GetDiagonalLeafChildren (uint currentNodeLocation, uint currentNodeParrentLocation) {
        var diagonalNeighbors = new List<OctreeNode> ();
        var currentNode = GetNode (currentNodeLocation);
        for (int i = 0; i < diagonalNeighborLocations.Length; i++) {
            for (int j = 0; j < diagonalNeighborLocations[i].Length; j++) {
                var nodeLocation = GetNodeIndexAtCoord (currentNode.chunk.coords + diagonalNeighborLocations[i][j]);
                var nodeParentLocation = nodeLocation >> 3;
                if (nodeParentLocation == currentNodeParrentLocation) continue;
                diagonalNeighbors.AddRange (GetChildren (nodeParentLocation));
            }
        }
        return diagonalNeighbors;
    }

    OctreeNode GetParentNode (OctreeNode node) {
        uint locCodeParent = node.locationCode >> 3;
        return nodes[locCodeParent];
    }

    public VoxelChunk GetChunkAtCoord (Vector3Int coord) {
        var nodeLocation = GetNodeIndexAtCoord (coord);
        if (nodeLocation <= 0) return null;

        return nodes[nodeLocation].chunk;
    }

    public static uint RelativeLocation (uint location, byte axis, bool direction) {
        byte depth = GetDepth (location);
        byte startDepth = depth;
        while (depth > 0) {
            uint depthAxisBit = (uint) (axis << ((startDepth - depth) * 3));
            uint checkAxisAtDepth = location & depthAxisBit;
            if ((!direction && checkAxisAtDepth > 0) || (direction && checkAxisAtDepth == 0)) {
                return location ^ depthAxisBit;
            } else {
                location ^= depthAxisBit;
            }
            depth--;
        }
        return 0;
    }

    public byte GetMaxDepth () => maxDepth;

    public static uint RelativeLeafNodeLocation (uint location, Vector3Int offset) {
        uint result = location;

        bool yDirection = offset.y > 0;
        int ySteps = Mathf.Abs (offset.y);
        for (int y = 0; y < ySteps; y++)
            result = RelativeLocation (result, 0b100, yDirection);

        bool zDirection = offset.z > 0;
        int zSteps = Mathf.Abs (offset.z);
        for (int z = 0; z < zSteps; z++)
            result = RelativeLocation (result, 0b010, zDirection);

        bool xDirection = offset.x > 0;
        int xSteps = Mathf.Abs (offset.x);
        for (int x = 0; x < xSteps; x++)
            result = RelativeLocation (result, 0b001, xDirection);

        return result;
    }

    byte GetNodeDepth (OctreeNode node) {
        return GetDepth (node.locationCode);
    }

    public static byte GetDepth (uint locationCode) {
        byte depth = 0;
        while (locationCode > 1) {
            depth++;
            locationCode = locationCode >> 3;
        }
        return depth;
    }

    void DrawAll (OctreeNode node) {
        foreach (KeyValuePair<uint, OctreeNode> entry in nodes) {
            Gizmos.color = new Color (1, 1, 1, .2f);
            Gizmos.DrawWireCube (entry.Value.bounds.center, entry.Value.bounds.size);
            // UnityEditor.Handles.Label (entry.Value.bounds.center, "NODE");
        }

    }
    public static bool isBitSet (int b, int bitNumber) => (b & (1 << bitNumber)) != 0;

    private string IntToBinaryString (uint number) {
        const uint mask = 1;
        var binary = string.Empty;
        while (number > 0) {
            // Logical AND the number and prepend it to the result string
            binary = (number & mask) + binary;
            number = number >> 1;
        }

        return binary;
    }
}