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
        static BayesianSpaceGenerator generator;
        static int rooms = 6;
        static int cpl = (int)Math.Floor((double)rooms / 2);

        [TestInitialize]
        public void TestInitialize()
        {
            testGraph = new SpaceGraph();

            for (int i = 0; i < rooms; i++)
            {
                testGraph.CreateNode(i);
            }

            generator = new BayesianSpaceGenerator();
            testGraph = generator.CriticalPathMapper(testGraph, cpl);
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
            bool isValid = generator.ValidCPLength(testGraph, testGraph.Entrance, testGraph.Goal);

            Assert.IsFalse(isValid);

            CollectionAssert.AreEqual(testGraph.CriticalPath, originalCP);
            CollectionAssert.AreEqual(originalEntrance.Edges, testGraph.Entrance.Edges);
            CollectionAssert.AreEqual(originalGoal.Edges, testGraph.Goal.Edges);


            testGraph.Connect(testGraph.Entrance, testGraph.Goal);

            CollectionAssert.AreNotEqual(testGraph.CriticalPath, originalCP);
        }

        [TestMethod()]
        public void CriticalPathMapperTest()
        {
            Trace.WriteLine(testGraph.ToString());
            Assert.AreEqual(cpl, testGraph.AllNodes.FindAll(node => node.CPDistance != null).Count);
            CollectionAssert.AreEqual(new List<int> {0,1,5},testGraph.CriticalPath);

        }
    }
}