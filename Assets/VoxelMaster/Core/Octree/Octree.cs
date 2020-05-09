using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelMaster.Chunk;

namespace VoxelMaster.Core {
    public class Octree<T> {

        private static readonly Vector3Int[][] diagonalNeighborLocations = new Vector3Int[][] {
        new Vector3Int[4] { new Vector3Int (-1, 1, -1), new Vector3Int (-1, 1, 1), new Vector3Int (1, 1, 1), new Vector3Int (1, 1, -1) },
        new Vector3Int[4] { new Vector3Int (-1, -1, -1), new Vector3Int (-1, -1, 1), new Vector3Int (1, -1, 1), new Vector3Int (1, -1, -1) }
    };

        int leafSize;
        byte maxDepth;

        Dictionary<uint, OctreeNode<T>> nodes = new Dictionary<uint, OctreeNode<T>>();

        public Octree(int leafSize, byte depth) {
            this.leafSize = leafSize;
            maxDepth = depth;

            var rootNode = new OctreeNode<T> {
                locationCode = 0b1,
                childrenFlags = 0x0,
                bounds = new Bounds(Vector3.zero, Vector3.one * (leafSize << depth)),
            };

            nodes.Add(0b1, rootNode);
        }

        private void AddNode(OctreeNode<T> node) {
            if (nodes.ContainsKey(node.locationCode)) throw new Exception("LocationCode already occupied");
            nodes.Add(node.locationCode, node);

        }

        private uint[] SplitNode(uint location) {
            var node = nodes[location];
            if (node == null) throw new Exception("Tried to split null node");


            uint[] childLocations = new uint[8];
            for (int i = 0; i < 8; i++) {
                uint childLocationCode = (node.locationCode << 3) | (uint)i;
                childLocations[i] = childLocationCode;


                var oneFourth = node.bounds.size.x / 4;
                var childOffset = new Vector3(
                    (i & 0b001) > 0 ? oneFourth : -oneFourth,
                    (i & 0b100) > 0 ? oneFourth : -oneFourth,
                    (i & 0b010) > 0 ? oneFourth : -oneFourth
                );

                var child = new OctreeNode<T> {
                    locationCode = (node.locationCode << 3) | childLocationCode,
                    childrenFlags = 0b0,
                    bounds = new Bounds(node.bounds.center + childOffset, node.bounds.size / 2),
                };
                AddNode(child);
                node.childrenFlags ^= (byte)(1 << i);
            }
            return childLocations;
        }

        private void SplitRecursive(uint nodeLocation, Vector3 pos, float distance) {
            if (!nodes.ContainsKey(nodeLocation)) return;

            var node = nodes[nodeLocation];
            if (node.bounds.size.x <= leafSize || node.bounds.SqrDistance(pos) > distance * distance) return;


            var children = SplitNode(node.locationCode);
            foreach (var child in children) {
                SplitRecursive(child, pos, distance);
            }


        }

        public void SplitFromDistance(Vector3 pos, float distance) {

            OctreeNode<T> node = nodes[0b1];

            if (!node.bounds.Contains(pos)) return;

            SplitRecursive(node.locationCode, pos, distance);

        }

        private byte GetChildLocationCode(Vector3 pos, OctreeNode<T> node) {
            byte locationCode = 0b000;
            if (pos.x > node.bounds.center.x) locationCode |= 0b001;
            if (pos.y > node.bounds.center.y) locationCode |= 0b100;
            if (pos.z > node.bounds.center.z) locationCode |= 0b010;
            return locationCode;
        }

        public uint GetNodeIndexAtCoord(Vector3Int coord) {
            // Debug.Log ($"Input coord: {coord}");
            OctreeNode<T> currentNode = nodes[0b1];

            byte currentDepth = 0;

            while (currentDepth < maxDepth) {
                currentDepth = GetNodeDepth(currentNode);
                byte childLocationCode = GetChildLocationCode(coord * leafSize + (Vector3.one * leafSize / 2), currentNode);

                bool hasChild = isBitSet(currentNode.childrenFlags, childLocationCode);
                if (!hasChild) return 0;

                currentNode = nodes[(currentNode.locationCode << 3) | childLocationCode];
            }

            Debug.Assert(currentDepth == maxDepth);
            // Debug.Log ($"output bounds: {currentNode.bounds}");
            return currentNode.locationCode;
        }

        public OctreeNode<T> GetNode(uint locationCode) {
            if (!nodes.ContainsKey(locationCode)) return null;
            return nodes[locationCode];

        }

        public void DrawLeafNodes() {
            DrawLeafNodes(nodes[0b1]);

        }

        private void DrawLeafNodes(OctreeNode<T> node) {
            var depth = GetNodeDepth(node);
            if (depth >= maxDepth + 1) {
                return;
            }
            Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
            Handles.Label(node.bounds.center, depth.ToString());

            for (int i = 0; i < 8; i++) {
                if (isBitSet(node.childrenFlags, i)) {
                    uint locCodeChild = (node.locationCode << 3) | (uint)i;
                    OctreeNode<T> child = nodes[locCodeChild];
                    DrawLeafNodes(child);
                }
            }
        }

        internal List<OctreeNode<T>> GetLeafChildren(uint locationCode) {
            var node = nodes[locationCode];

            var chunks = new List<OctreeNode<T>>();

            if (node.childrenFlags <= 0) {
                chunks.Add(node);
                return chunks;
            }

            for (int i = 0; i < 8; i++) {
                if (isBitSet(node.childrenFlags, i)) {
                    uint locCodeChild = (node.locationCode << 3) | (uint)i;
                    chunks.AddRange(GetLeafChildren(locCodeChild));
                }
            }
            return chunks;
        }

        public List<OctreeNode<T>> GetChildren(uint locationCode) {
            var node = nodes[locationCode];

            var chunks = new List<OctreeNode<T>>();

            if (GetNodeDepth(node) >= maxDepth + 1) {
                return chunks;
            }

            for (int i = 0; i < 8; i++) {
                if (isBitSet(node.childrenFlags, i)) {
                    uint locCodeChild = (node.locationCode << 3) | (uint)i;
                    chunks.Add(GetNode(locCodeChild));
                }
            }
            return chunks;
        }

        public void Reset() {
            nodes.Clear();

            var rootNode = new OctreeNode<T> {
                locationCode = 0b1,
                childrenFlags = 0x0,
                bounds = new Bounds(Vector3.zero, Vector3.one * (leafSize << maxDepth)),
            };

            nodes.Add(0b1, rootNode);
        }

        public List<uint> GetChildLocations(uint locationCode) {
            var node = this.nodes[locationCode];
            var nodes = new List<uint>();
            if (GetNodeDepth(node) >= maxDepth)
                return nodes;

            for (int i = 0; i < 8; i++) {
                if (isBitSet(node.childrenFlags, i)) {
                    uint locCodeChild = (node.locationCode << 3) | (uint)i;
                    nodes.Add(locCodeChild);
                }
            }
            return nodes;
        }

        //public List<uint> GetDiagonalNeighbours(uint currentNodeLocation, byte distance) {
        //    var diagonalNeighbors = new List<uint>();
        //    var currentNode = GetNode(currentNodeLocation);
        //    for (int i = 0; i < diagonalNeighborLocations.Length; i++) {
        //        for (int j = 0; j < diagonalNeighborLocations[i].Length; j++) {
        //            var nodeLocation = GetNodeIndexAtCoord(currentNode.item.coords + diagonalNeighborLocations[i][j] * distance);
        //            diagonalNeighbors.Add(nodeLocation);
        //        }
        //    }
        //    return diagonalNeighbors;
        //}

        OctreeNode<T> GetParentNode(OctreeNode<T> node) {
            uint locCodeParent = node.locationCode >> 3;
            return nodes[locCodeParent];
        }

        public T GetChunkAtCoord(Vector3Int coord) {
            var nodeLocation = GetNodeIndexAtCoord(coord);
            if (nodeLocation <= 0) return default(T);

            return nodes[nodeLocation].item;
        }

        public static uint RelativeLocation(uint location, byte axis, bool direction) {
            byte depth = GetDepth(location);
            byte startDepth = depth;
            while (depth > 0) {
                uint depthAxisBit = (uint)(axis << ((startDepth - depth) * 3));
                uint checkAxisAtDepth = location & depthAxisBit;
                if ((!direction && checkAxisAtDepth > 0) || (direction && checkAxisAtDepth == 0)) {
                    return location ^ depthAxisBit;
                }
                else {
                    location ^= depthAxisBit;
                }
                depth--;
            }
            return 0;
        }

        public byte GetMaxDepth() => maxDepth;

        public static uint RelativeLeafNodeLocation(uint location, Vector3Int offset) {
            uint result = location;

            bool yDirection = offset.y > 0;
            int ySteps = Mathf.Abs(offset.y);
            for (int y = 0; y < ySteps; y++)
                result = RelativeLocation(result, 0b100, yDirection);

            bool zDirection = offset.z > 0;
            int zSteps = Mathf.Abs(offset.z);
            for (int z = 0; z < zSteps; z++)
                result = RelativeLocation(result, 0b010, zDirection);

            bool xDirection = offset.x > 0;
            int xSteps = Mathf.Abs(offset.x);
            for (int x = 0; x < xSteps; x++)
                result = RelativeLocation(result, 0b001, xDirection);

            return result;
        }

        byte GetNodeDepth(OctreeNode<T> node) {
            return GetDepth(node.locationCode);
        }

        public static byte GetDepth(uint locationCode) {
            byte depth = 0;
            while (locationCode > 1) {
                depth++;
                locationCode >>= 3;
            }
            return depth;
        }

        public void DrawAll() {
            foreach (KeyValuePair<uint, OctreeNode<T>> entry in nodes) {
                Gizmos.DrawWireCube(entry.Value.bounds.center, entry.Value.bounds.size);
            }

        }
        public static bool isBitSet(int b, int bitNumber) => (b & (1 << bitNumber)) != 0;

        private string IntToBinaryString(uint number) {
            const uint mask = 1;
            var binary = string.Empty;
            while (number > 0) {
                // Logical AND the number and prepend it to the result string
                binary = (number & mask) + binary;
                number = number >> 1;
            }

            return binary;
        }

        static readonly Vector3Int vector3IntForward = new Vector3Int(0, 0, 1);
        private readonly Vector3Int[] extractionAxis = new Vector3Int[] {
        Vector3Int.right,
        Vector3Int.up,
        vector3IntForward,
        Vector3Int.right + Vector3Int.up + vector3IntForward,
        Vector3Int.right + Vector3Int.up,
        Vector3Int.up + vector3IntForward,
        Vector3Int.right + vector3IntForward,
    };

        public bool IsLeafNode(uint locationCode) {
            return GetDepth(locationCode) >= (maxDepth);
        }



    }
}