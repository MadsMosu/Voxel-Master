using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {
    public class BenchmarkTests {

        // Coords are 0b1_YZX_YZX_YZX
        uint RelativeLocation (uint location, byte axis, bool direction) {
            byte depth = GetDepth (location);
            byte startDepth = depth;
            while (depth > 0) {
                uint depthAxisBit = (uint) (axis << ((startDepth - depth) * 3));
                uint checkAxisAtDepth = location & depthAxisBit;
                if ((!direction && checkAxisAtDepth > 0) || (direction && checkAxisAtDepth == 0)) {
                    Debug.Log (System.Convert.ToString (location, 2));
                    Debug.Log (System.Convert.ToString (location ^ depthAxisBit, 2));
                    return location ^ depthAxisBit;
                } else {
                    location ^= depthAxisBit;
                }
                depth--;
            }
            return 0;
        }

        byte GetDepth (uint locationCode) {
            byte depth = 0;
            while (locationCode > 1) {
                depth++;
                locationCode = locationCode >> 3;
            }
            return depth;
        }

        [Test]
        public void SpecificTestCase () {
            uint startCode = 0b1111000000000000000000000000000;
            uint expected_ = 0b1110001001001001001001001001001;
            uint neighbour = RelativeLocation (startCode, 0b001, false);
            Assert.AreEqual (expected_, neighbour);
        }

        [Test]
        public void SpecificTestCase2 () {
            uint startCode = 0b1111000000000000000000000000000;
            uint expected_ = 0b1111000000000000000000000000001;
            uint neighbour = RelativeLocation (startCode, 0b001, true);
            Assert.AreEqual (expected_, neighbour);
        }

        [Test]
        public void GetPositiveXNeighbour () {

            uint startCode = 0b1_111_000;
            uint neighbour = RelativeLocation (startCode, 0b001, true);
            Assert.AreEqual (0b1_111_001, neighbour);
        }

        [Test]
        public void GetPositiveXNeighbourEdge () {
            uint startCode = 0b1_000_001;
            uint neighbour = RelativeLocation (startCode, 0b001, true);
            Assert.AreEqual (0b1_001_000, neighbour);
        }

        [Test]
        public void GetPositiveXNeighbourEdgeOutOfBounds () {
            uint startCode = 0b1_001_001;
            uint neighbour = RelativeLocation (startCode, 0b001, true);
            Assert.AreEqual (0, neighbour);
        }

        [Test]
        public void GetNegativeXNeighbour () {
            uint startCode = 0b1_111_001;
            uint neighbour = RelativeLocation (startCode, 0b001, false);
            Assert.AreEqual (0b1_111_000, neighbour);
        }

        [Test]
        public void GetNegativeXNeighbourEdge () {
            uint startCode = 0b1_001_000;
            uint neighbour = RelativeLocation (startCode, 0b001, false);
            Assert.AreEqual (0b1_000_001, neighbour);
        }

        [Test]
        public void GetNegativeXNeighbourEdgeOutOfBounds () {
            uint startCode = 0b1_000_000;
            uint neighbour = RelativeLocation (startCode, 0b001, false);
            Assert.AreEqual (0, neighbour);
        }

        [Test]
        public void GetPositiveYNeighbour () {

            uint startCode = 0b1_111_000;
            uint neighbour = RelativeLocation (startCode, 0b100, true);
            Assert.AreEqual (0b1_111_100, neighbour);
        }

        [Test]
        public void GetPositiveYNeighbourEdge () {
            uint startCode = 0b1_000_100;
            uint neighbour = RelativeLocation (startCode, 0b100, true);
            Assert.AreEqual (0b1_100_000, neighbour);
        }

        [Test]
        public void GetPositiveYNeighbourEdgeOutOfBounds () {
            uint startCode = 0b1_101_101;
            uint neighbour = RelativeLocation (startCode, 0b100, true);
            Assert.AreEqual (0, neighbour);
        }

        [Test]
        public void GetNegativeYNeighbour () {
            uint startCode = 0b1_111_100;
            uint neighbour = RelativeLocation (startCode, 0b100, false);
            Assert.AreEqual (0b1_111_000, neighbour);
        }

        [Test]
        public void GetNegativeYNeighbourEdge () {
            uint startCode = 0b1_100_000;
            uint neighbour = RelativeLocation (startCode, 0b100, false);
            Assert.AreEqual (0b1_000_100, neighbour);
        }

        [Test]
        public void GetNegativeYNeighbourEdgeOutOfBounds () {
            uint startCode = 0b1_000_000;
            uint neighbour = RelativeLocation (startCode, 0b100, false);
            Assert.AreEqual (0, neighbour);
        }

        [Test]
        public void GetPositiveZNeighbour () {

            uint startCode = 0b1_111_000;
            uint neighbour = RelativeLocation (startCode, 0b010, true);
            Assert.AreEqual (0b1_111_010, neighbour);
        }

        [Test]
        public void GetPositiveZNeighbourEdge () {
            uint startCode = 0b1_000_010;
            uint neighbour = RelativeLocation (startCode, 0b010, true);
            Assert.AreEqual (0b1_010_000, neighbour);
        }

        [Test]
        public void GetPositiveZNeighbourEdgeOutOfBounds () {
            uint startCode = 0b1_111_111;
            uint neighbour = RelativeLocation (startCode, 0b010, true);
            Assert.AreEqual (0, neighbour);
        }

        [Test]
        public void GetNegativeZNeighbour () {
            uint startCode = 0b1_111_010;
            uint neighbour = RelativeLocation (startCode, 0b010, false);
            Assert.AreEqual (0b1_111_000, neighbour);
        }

        [Test]
        public void GetNegativeZNeighbourEdge () {
            uint startCode = 0b1_010_000;
            uint neighbour = RelativeLocation (startCode, 0b010, false);
            Assert.AreEqual (0b1_000_010, neighbour);
        }

        [Test]
        public void GetNegativeZNeighbourEdgeOutOfBounds () {
            uint startCode = 0b1_000_000;
            uint neighbour = RelativeLocation (startCode, 0b010, false);
            Assert.AreEqual (0, neighbour);
        }

    }
}