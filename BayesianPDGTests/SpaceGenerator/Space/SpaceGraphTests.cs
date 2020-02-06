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


            testGraph.Connect(0, 1);
            testGraph.Connect(1, 2);
            testGraph.Connect(2, 3);
            testGraph.Connect(3, 4);
        }

        [TestMethod()]
        public void InitialiseCPTest()
        {
            var actual = testGraph.CriticalPath;
            var expected = new int[] { 0, 1, 2, 3, 4 }.ToList();

            Trace.WriteLine(testGraph.ToString());
            Assert.IsTrue(!actual.Except(expected).ToList().Any() && !expected.Except(actual).ToList().Any());

        }

        [TestMethod()]
        public void ChangeCPTest()
        {
            testGraph.Connect(0, 4);
            var actual = testGraph.CriticalPath;
            var expected = new int[] { 0, 4 }.ToList();

            Trace.WriteLine(testGraph.ToString());
            Assert.IsTrue(!actual.Except(expected).ToList().Any() && !expected.Except(actual).ToList().Any());

        }

        [TestMethod()]
        public void ConnectTest()
        {
            SpaceGraph basic = new SpaceGraph();
            basic.CreateNode(0);
            basic.CreateNode(1);
            basic.CreateNode(2);
            basic.Connect(0, 1);
            Assert.IsTrue(basic.Node(0).IsConnected(basic.Node(1)));
            Assert.IsTrue(basic.Node(1).IsConnected(basic.Node(0)));
            Assert.IsFalse(basic.Node(0).IsConnected(basic.Node(2)));
        }

        [TestMethod()]
        public void BidirectionalConnectTest()
        {
            SpaceGraph basic = new SpaceGraph();
            basic.CreateNode(0);
            basic.CreateNode(1);
            basic.Connect(0, 1);
            basic.Connect(1, 0);
            Assert.IsTrue(basic.Node(0).IsConnected(basic.Node(1)));
            Assert.IsTrue(basic.Node(1).IsConnected(basic.Node(0)));

            Assert.AreEqual(1, basic.Node(0).Edges.Count);
            Assert.AreEqual(1, basic.Node(1).Edges.Count);
        }

        [TestMethod()]
        public void DisconnectTest()
        {
            SpaceGraph basic = new SpaceGraph();
            basic.CreateNode(0);
            basic.CreateNode(1);
            basic.Connect(0, 1);
            Assert.IsTrue(basic.Node(0).IsConnected(basic.Node(1)));
            Assert.IsTrue(basic.Node(1).IsConnected(basic.Node(0)));

            basic.Disconnect(0, 1);
            Assert.IsFalse(basic.Node(0).IsConnected(basic.Node(1)));
            Assert.IsFalse(basic.Node(1).IsConnected(basic.Node(0)));
        }

        [TestMethod()]
        public void BidirectionalDisconnectTest()
        {
            SpaceGraph basic = new SpaceGraph();
            basic.CreateNode(0);
            basic.CreateNode(1);
            //0->1
            basic.Connect(0, 1);
            basic.Connect(1, 0);
            Assert.IsTrue(basic.Node(0).IsConnected(basic.Node(1)));
            Assert.IsTrue(basic.Node(1).IsConnected(basic.Node(0)));


            basic.Disconnect(0, 1);

            Assert.AreEqual(0, basic.Node(0).Edges.Count);
            Assert.AreEqual(0, basic.Node(1).Edges.Count);

            //1->0
            basic.Connect(0, 1);
            basic.Connect(1, 0);
            Assert.IsTrue(basic.Node(0).IsConnected(basic.Node(1)));
            Assert.IsTrue(basic.Node(1).IsConnected(basic.Node(0)));

            basic.Disconnect(1, 0);

            Assert.AreEqual(0, basic.Node(0).Edges.Count);
            Assert.AreEqual(0, basic.Node(1).Edges.Count);
        }

        [TestMethod()]
        public void IsCompletedTest()
        {
            SpaceGraph incomplete = new SpaceGraph();

            Trace.WriteLine(testGraph.ToString());
            Assert.IsTrue(testGraph.isComplete);

            incomplete.CreateNode(0);
            incomplete.CreateNode(1);
            incomplete.CreateNode(2);
            incomplete.CreateNode(3);

            incomplete.Connect(0, 3);
            incomplete.Connect(1, 2);

            Trace.WriteLine(incomplete.ToString());

            Assert.IsFalse(incomplete.isComplete);
        }


        [TestMethod()]
        public void PathToTest()
        {
            SpaceGraph validGraph = new SpaceGraph();

            for (int i = 0; i < 6; i++)
            {
                validGraph.CreateNode(i);
            }

            validGraph.Connect(0, 2);
            validGraph.Connect(0, 5);
            validGraph.Connect(1, 4);
            validGraph.Connect(3, 5);
            validGraph.Connect(4, 5);

            Trace.WriteLine(validGraph.ToString());

            Assert.AreEqual(4, validGraph.PathTo(0, 1).Count);
            CollectionAssert.AreEqual(new int[] { 0, 5, 4, 1 }.ToList(), validGraph.PathTo(0, 1));


        }
       
    }
}