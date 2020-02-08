using Microsoft.VisualStudio.TestTools.UnitTesting;
using BayesianPDG.SpaceGenerator.CSP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BayesianPDG.SpaceGenerator.Space;
using BayesianPDG.Utils;
using System.Diagnostics;

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
                node.Values = new List<List<Node>>(node.MaxNeighbours.Value);
            });

            foreach (Node node in testGraph.AllNodes)
            {
                var neighbourCombinations = Combinator.Combinations(testGraph.AllNodes, node.MaxNeighbours.Value);
                foreach (IEnumerable<Node> combination in neighbourCombinations)
                {
                    if (!combination.ToList().Contains(node))
                    {
                        node.Values.Add(combination.ToList());
                    }
                }
            }

        }

        [TestMethod()]
        public void ConstraintModelTest()
        {
            testGraph.AllNodes.ForEach(node => Trace.WriteLine(node.PrintConnections()));
            ConstraintModel CSModel = new ConstraintModel(testGraph);

           // throw new NotImplementedException();
        }
    }
}