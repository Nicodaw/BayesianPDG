using Microsoft.VisualStudio.TestTools.UnitTesting;
using BayesianPDG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using BayesianPDG.SpaceGenerator.Space;

namespace BayesianPDG.Utils.Tests
{
    [TestClass()]
    public class CombinatorTests
    {
        private SpaceGraph testGraph;
        private static int rooms = 6;

        [TestInitialize]
        public void TestInitialize()
        {
            testGraph = new SpaceGraph();
            List<(int, int, int)> roomParams = new List<(int, int, int)>() { (0, 0, 1), (0, 1, 4), (2, 3, 1), (1, 2, 1), (1, 2, 1), (0, 2, 1)};

            for (int i = 0; i < rooms; i++)
            {
                testGraph.CreateNode(i);
            }

            testGraph.AllNodes.ForEach(node =>
            {
                node.CPDistance = roomParams[node.Id].Item1;
                node.Depth = roomParams[node.Id].Item2;
                node.MaxNeighbours = roomParams[node.Id].Item3;
            });
        }

        [TestMethod()]
        public void CombinationsTest()
        {
            List<int> lst = new List<int> { 0, 1, 2 };
            List<List<int>> expected = new List<List<int>> 
            {
                new List<int>{0,1}, new List<int> { 0, 2 }, new List<int> { 0, 2 },
                new List<int>{1,2}, new List<int> { 1, 3 }, new List<int> { 2, 3 }
            };
            var combinations = Combinator.Combinations(lst, 2);
            List<List<int>> actual = new List<List<int>>();


            CollectionAssert.AreEqual(expected, actual);

            throw new NotImplementedException();


        }

        [TestMethod()]
        public void NonSelfReferentialRoomCombinationsTest()
        {
            foreach (Node node in testGraph.AllNodes)
            {
                var neighbourCombinations = Combinator.Combinations(testGraph.AllNodes, node.MaxNeighbours.Value);
                foreach (IEnumerable<Node> combination in neighbourCombinations)
                {
                    if (!combination.ToHashSet().Contains(node))
                    {
                        node.Values.Add(combination.ToHashSet());
                    }
                }
            }
            throw new NotImplementedException();
        }
    }
}