using Microsoft.VisualStudio.TestTools.UnitTesting;
using BayesianPDG.SpaceGenerator.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BayesianPDG.SpaceGenerator.Space.Tests
{
    [TestClass()]
    public class NodeTests
    {
        Node A;
        Node B;
        [TestInitialize]
        public void TestInitialize()
        {
            A = new Node(0);
            B = new Node(1);
        }


        [TestMethod()]
        public void IsConnectedTest()
        {
            Assert.IsFalse(A.IsConnected(B));

            A.AddEdge(B);

            Assert.IsTrue(A.IsConnected(B));
            Assert.IsTrue(B.IsConnected(A));
        }

        [TestMethod()]
        public void NodeTest()
        {
            Assert.IsTrue(A.CPDistance == 0);
            Assert.IsTrue(B.CPDistance == null);
        }

        [TestMethod()]
        public void AddEdgeTest()
        {
            CollectionAssert.AreEqual(A.Edges, new List<Edge>());
            CollectionAssert.AreEqual(B.Edges, new List<Edge>());

            Edge expectedA = new Edge();
            expectedA.Parent = A;
            expectedA.Child = B;

            Edge expectedB = new Edge();
            expectedB.Parent = B;
            expectedB.Child = A;

            A.AddEdge(B);

            Assert.IsTrue(A.Edges.Count == 1);
            Assert.IsTrue(B.Edges.Count == 1);

            Assert.IsTrue(A.Edges.Contains(expectedA));
            Assert.IsTrue(B.Edges.Contains(expectedB));
        }


        [TestMethod()]
        public void RemoveEdgeTest()
        {
            A.AddEdge(B);

            Edge expectedA = new Edge();
            expectedA.Parent = A;
            expectedA.Child = B;

            Edge expectedB = new Edge();
            expectedB.Parent = B;
            expectedB.Child = A;

            Assert.IsTrue(A.Edges.Count == 1);
            Assert.IsTrue(B.Edges.Count == 1);

            A.RemoveEdge(B);
            CollectionAssert.AreEqual(A.Edges, new List<Edge>());
            CollectionAssert.AreEqual(B.Edges, new List<Edge>());
        }

        [TestMethod()]
        public void ValuesEqualTest()
        {
            Node C = new Node(3);
            Node D = new Node(4);
            Node E = new Node(5);

            Node C2 = new Node(3);
            Node D2 = new Node(4);
            Node E2 = new Node(5);

            List<Node> val1 = new List<Node> { C, D, E };
            List<Node> val2 = new List<Node> { C2, D2, E2};

            A.Values = new List<List<Node>>() { val1 };
            B.Values = new List<List<Node>>() { val2 };

            Assert.IsTrue(A.ValuesEqual(B));
        }

        [TestMethod()]
        public void ValuessDontEqualTest()
        {
            Node C = new Node(3);
            Node D = new Node(4);
            Node E = new Node(6);

            Node C2 = new Node(3);
            Node D2 = new Node(4);
            Node E2 = new Node(5);

            List<Node> val1 = new List<Node> { C, D, E };
            List<Node> val2 = new List<Node> { C2, D2, E2 };

            A.Values = new List<List<Node>>() { val1 };
            B.Values = new List<List<Node>>() { val2 };

            Assert.IsFalse(A.ValuesEqual(B));
        }

        [TestMethod()]
        public void ValuesRearrangedEqualTest()
        {
            Node C = new Node(3);
            Node D = new Node(4);
            Node E = new Node(5);

            Node C2 = new Node(3);
            Node D2 = new Node(4);
            Node E2 = new Node(5);

            List<Node> val1 = new List<Node> { C, D, E };
            List<Node> val2 = new List<Node> { E2, C2, D2 };

            A.Values = new List<List<Node>>() { val1 };
            B.Values = new List<List<Node>>() { val2 };

            Assert.IsTrue(A.ValuesEqual(B));
        }
    }
}