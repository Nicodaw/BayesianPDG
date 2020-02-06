using Microsoft.VisualStudio.TestTools.UnitTesting;
using BayesianPDG.SpaceGenerator.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BayesianPDG.SpaceGenerator.Space.Tests
{
    [TestClass()]
    public class SpaceGraphTests
    {
        static SpaceGraph testGraph;

        [TestInitialize]
        public void TestInitialize()
        {
            testGraph = new SpaceGraph();

            for (int i = 0; i < 5; i++)
            {
                testGraph.CreateNode(i);

                int cpd = (i < 3) ? 0 : i;
                int depth = (i == 0) ? 0 : i;

                testGraph.Node(i).CPDistance = cpd;
                testGraph.Node(i).Depth = depth;

            }
        }

        [TestMethod()]
        public void InitialiseCPTest()
        {
            testGraph.Connect(0, 1);
            testGraph.Connect(1, 2);
            testGraph.Connect(2, 3);
            testGraph.Connect(3, 4);
            var actual = testGraph.CriticalPath;
            var expected = new int[] { 0, 1, 2, 3, 4 }.ToList();

            Trace.WriteLine(testGraph.ToString());
            Assert.IsTrue(!actual.Except(expected).ToList().Any() && !expected.Except(actual).ToList().Any());

        }

        [TestMethod()]
        public void ChangeCPTest()
        {
            testGraph.Connect(0, 1);
            testGraph.Connect(1, 2);
            testGraph.Connect(2, 3);
            testGraph.Connect(3, 4);
            testGraph.Connect(0, 4);
            var actual = testGraph.CriticalPath;
            var expected = new int[] { 0, 4 }.ToList();

            Trace.WriteLine(testGraph.ToString());
            Assert.IsTrue(!actual.Except(expected).ToList().Any() && !expected.Except(actual).ToList().Any());

        }

        [TestMethod()]
        public void ConnectTest()
        {
            testGraph.Connect(0, 1);
            Assert.IsTrue(testGraph.Node(0).IsConnected(testGraph.Node(1)));
            Assert.IsTrue(testGraph.Node(1).IsConnected(testGraph.Node(0)));
            Assert.IsFalse(testGraph.Node(0).IsConnected(testGraph.Node(2)));
        }

        [TestMethod()]
        public void BidirectionalConnectTest()
        {
            testGraph.Connect(0, 1);
            testGraph.Connect(1, 0);
            Assert.IsTrue(testGraph.Node(0).IsConnected(testGraph.Node(1)));
            Assert.IsTrue(testGraph.Node(1).IsConnected(testGraph.Node(0)));

            Assert.AreEqual(1, testGraph.Node(0).Edges.Count);
            Assert.AreEqual(1, testGraph.Node(1).Edges.Count);
        }

        [TestMethod()]
        public void DisconnectTest()
        {
            testGraph.Connect(0, 1);
            Assert.IsTrue(testGraph.Node(0).IsConnected(testGraph.Node(1)));
            Assert.IsTrue(testGraph.Node(1).IsConnected(testGraph.Node(0)));

            testGraph.Disconnect(0, 1);
            Assert.IsFalse(testGraph.Node(0).IsConnected(testGraph.Node(1)));
            Assert.IsFalse(testGraph.Node(1).IsConnected(testGraph.Node(0)));
        }

        [TestMethod()]
        public void BidirectionalDisconnectTest()
        {   
            //0->1
            testGraph.Connect(0, 1);
            testGraph.Connect(1, 0);
            Assert.IsTrue(testGraph.Node(0).IsConnected(testGraph.Node(1)));
            Assert.IsTrue(testGraph.Node(1).IsConnected(testGraph.Node(0)));

            
            testGraph.Disconnect(0, 1);

            Assert.AreEqual(0, testGraph.Node(0).Edges.Count);
            Assert.AreEqual(0, testGraph.Node(1).Edges.Count);

            //1->0
            testGraph.Connect(0, 1);
            testGraph.Connect(1, 0);
            Assert.IsTrue(testGraph.Node(0).IsConnected(testGraph.Node(1)));
            Assert.IsTrue(testGraph.Node(1).IsConnected(testGraph.Node(0)));

            testGraph.Disconnect(1, 0);

            Assert.AreEqual(0, testGraph.Node(0).Edges.Count);
            Assert.AreEqual(0, testGraph.Node(1).Edges.Count);
        }
    }
}