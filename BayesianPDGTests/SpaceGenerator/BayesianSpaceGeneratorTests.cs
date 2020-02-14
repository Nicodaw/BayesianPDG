using BayesianPDG.SpaceGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [TestMethod()]
        public void CriticalPathMapperTest()
        {
            Trace.WriteLine(testGraph.ToString());
            Assert.AreEqual(cpl, testGraph.AllNodes.FindAll(node => node.CPDistance != null).Count);
            CollectionAssert.AreEqual(new List<int> { 0, 1, 5 }, testGraph.CriticalPath);

        }

        [TestMethod()]
        public void MapTest()
        {
            BayesianSpaceGenerator gen = new BayesianSpaceGenerator();
            gen.DungeonGraph = new SpaceGraph();

            for (int i = 0; i < 4; i++)
            {
                gen.DungeonGraph.CreateNode(i);
            }
            List<(int, int, int)> roomParams = new List<(int, int, int)>() { (0, 0, 1), (0, 1, 3), (1, 2, 1), (0, 2, 1) };
            gen.DungeonGraph.AllNodes.ForEach(node =>
            {
                node.CPDistance = roomParams[node.Id].Item1;
                node.Depth = roomParams[node.Id].Item2;
                node.MaxNeighbours = roomParams[node.Id].Item3;
            });
            gen.CriticalPathMapper(gen.DungeonGraph, 3);
            gen.DungeonGraph.ReducePotentialValues();
            Trace.WriteLine("Before");
            gen.DungeonGraph.AllNodes.ForEach(room => Trace.WriteLine(room.PrintConnections()));
            //       2
            //       |
            // 0 --- 1 --- 3  is the only valid config
            //
            //
            gen.Map();
            Trace.WriteLine("After");
            gen.DungeonGraph.AllNodes.ForEach(room => Trace.WriteLine(room.PrintConnections()));
            CollectionAssert.AreEqual(new int[] { 1, 0, 2, 3, 1, 1 }.ToList(), gen.DungeonGraph.AllNodes.SelectMany(x => x.Values.SelectMany(y => y.Select(z => z.Id))).ToList());
        }
    }
}