using Microsoft.VisualStudio.TestTools.UnitTesting;
using BayesianPDG.SpaceGenerator.CSP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BayesianPDG.SpaceGenerator.Space;

namespace BayesianPDG.SpaceGenerator.CSP.Tests
{
    [TestClass()]
    public class ConstraintModelTests
    {
        private SpaceGraph testGraph;
        private BayesianSpaceGenerator generator;
        private static int rooms = 6;
        private static int cpl = 3;

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

            List<(int, int, int)> roomParams = new List<(int, int, int)>() { (0, 0, 1), (0, 1, 4), (2, 3, 1), (1, 2, 1), (1, 2, 1), (0, 2, 1) };
            testGraph.AllNodes.ForEach(node =>
            {
                node.CPDistance = roomParams[node.Id].Item1;
                node.Depth = roomParams[node.Id].Item2;
                node.MaxNeighbours = roomParams[node.Id].Item3;
            });
        }

        [TestMethod()]
        public void ConstraintModelTest()
        {
            ConstraintModel CSModel = new ConstraintModel(testGraph);
        }
    }
}