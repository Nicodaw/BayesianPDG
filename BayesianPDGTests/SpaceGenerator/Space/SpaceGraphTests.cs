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
    }
}