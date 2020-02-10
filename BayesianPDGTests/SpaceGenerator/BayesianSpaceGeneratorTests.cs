﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public void NeighbourMapperTest()
        {
            throw new NotImplementedException();
            List<(int, int, int)> roomParams = new List<(int, int, int)>() { (0, 0, 1), (0, 1, 4), (2, 3, 1), (1, 2, 1), (1, 2, 1), (0, 2, 1) };
            testGraph.AllNodes.ForEach(node =>
            {
                node.CPDistance = roomParams[node.Id].Item1;
                node.Depth = roomParams[node.Id].Item2;
                node.MaxNeighbours = roomParams[node.Id].Item3;
            });
            testGraph = generator.NeighbourMapper(testGraph);

            testGraph.AllNodes.ForEach(node => Assert.AreEqual(node.MaxNeighbours, node.Edges.Count()));
        }
    }
}