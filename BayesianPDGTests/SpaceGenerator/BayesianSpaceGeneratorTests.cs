using Microsoft.VisualStudio.TestTools.UnitTesting;
using BayesianPDG.SpaceGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BayesianPDG.SpaceGenerator.Space;
using System.Diagnostics;

namespace BayesianPDG.SpaceGenerator.Tests
{
    [TestClass]
    public class BayesianSpaceGeneratorTests
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
                int neighbours = (i == 0 || i == 4) ? 1 : i;

                testGraph.Node(i).CPDistance = cpd;
                testGraph.Node(i).Depth = depth;
                testGraph.Node(i).MaxNeighbours = neighbours;

            }
            testGraph.Connect(0, 1);
            testGraph.Connect(1, 2);
            testGraph.Connect(2, 3);
            testGraph.Connect(3, 4);
            Trace.WriteLine(testGraph.ToString());


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
            BayesianSpaceGenerator generator = new BayesianSpaceGenerator();
            bool isValid = generator.ValidCPLength(testGraph, testGraph.Entrance, testGraph.Goal);

            Assert.IsFalse(isValid);

            CollectionAssert.AreEqual(testGraph.CriticalPath, originalCP);
            CollectionAssert.AreEqual(originalEntrance.Edges, testGraph.Entrance.Edges);
            CollectionAssert.AreEqual(originalGoal.Edges, testGraph.Goal.Edges);


            testGraph.Connect(testGraph.Entrance, testGraph.Goal);

            CollectionAssert.AreNotEqual(testGraph.CriticalPath, originalCP);
        }


    }
}