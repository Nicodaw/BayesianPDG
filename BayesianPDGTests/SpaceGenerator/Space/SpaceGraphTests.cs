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
                int maxN = (i == 0 || i == 4) ? 1 : 2;

                testGraph.Node(i).CPDistance = cpd;
                testGraph.Node(i).Depth = depth;
                testGraph.Node(i).MaxNeighbours = maxN;
            }
            //node: neighbours
            // 1 : 1
            // 2 : 2
            // 3 : 2
            // 4 : 1

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

        [TestMethod]
        public void ValidCPLengthTest()
        {
            var originalCP = testGraph.CriticalPath;
            Node originalEntrance = new Node()
            {

                Edges = testGraph.Entrance.Edges,
                Depth = testGraph.Entrance.Depth,
                MaxNeighbours = testGraph.Entrance.MaxNeighbours,
                CPDistance = testGraph.Entrance.CPDistance,
                Id = testGraph.Entrance.Id
            };
            Node originalGoal = new Node()
            {
                Edges = testGraph.Goal.Edges,
                Depth = testGraph.Goal.Depth,
                MaxNeighbours = testGraph.Goal.MaxNeighbours,
                CPDistance = testGraph.Goal.CPDistance,
                Id = testGraph.Goal.Id
            };
            bool isValid = testGraph.ValidCPLength(testGraph.Entrance, testGraph.Goal);

            Assert.IsFalse(isValid);

            CollectionAssert.AreEqual(testGraph.CriticalPath, originalCP);
            CollectionAssert.AreEqual(originalEntrance.Edges, testGraph.Entrance.Edges);
            CollectionAssert.AreEqual(originalGoal.Edges, testGraph.Goal.Edges);


            testGraph.Connect(testGraph.Entrance, testGraph.Goal);

            CollectionAssert.AreNotEqual(testGraph.CriticalPath, originalCP);
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

        [TestMethod()]
        public void ReducePotentialValuesTest()
        {
            SpaceGraph valueGraph = new SpaceGraph();

            for (int i = 0; i < 6; i++)
            {
                valueGraph.CreateNode(i);
            }

            List<(int, int, int)> roomParams = new List<(int, int, int)>() { (0, 0, 1), (0, 1, 4), (2, 3, 1), (1, 2, 1), (1, 2, 1), (0, 2, 1) };
            valueGraph.AllNodes.ForEach(node =>
            {
                node.CPDistance = roomParams[node.Id].Item1;
                node.Depth = roomParams[node.Id].Item2;
                node.MaxNeighbours = roomParams[node.Id].Item3;
            });

            //map CP
            valueGraph.Connect(0, 1);
            valueGraph.Connect(1, 5);

            valueGraph.ReducePotentialValues();

            List<int> actual = valueGraph.AllNodes.SelectMany(x => x.Values.SelectMany(y => y.Select(z => z.Id))).ToList();
            valueGraph.AllNodes.ForEach(node => Trace.WriteLine(node.PrintConnections()));
            Trace.WriteLine(string.Join(", ", actual));
            ///////////////////////////////////  [0][                 1                ][    2       ][     3       ][       4     ][5]
            CollectionAssert.AreEqual(new int[] { 1, 0, 2, 3, 5, 0, 2, 4, 5, 0, 3, 4, 5, 0, 1, 3, 4, 5, 0, 1, 2, 4, 5, 0, 1, 2, 3, 5, 1 }.ToList(), actual);
        }
    }
}