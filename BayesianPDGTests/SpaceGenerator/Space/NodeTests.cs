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
    }
}