using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace BayesianPDG.Utils.Tests
{
    [TestClass()]
    public class CombinatorTests
    {
        [TestMethod()]
        public void CombinationsTest()
        {
            List<int> lst = new List<int> { 0, 1, 2, 3 };
            List<List<int>> expected = new List<List<int>>
            {
               new List<int> {0,1}, new List<int> { 0, 2 }, new List<int> { 0, 3 },
                new List<int>{1,2}, new List<int> { 1, 3 }, new List<int> { 2, 3 }
            };
            var combinations = Combinator.Combinations(lst, 2);
            List<List<int>> actual = new List<List<int>>();

            foreach (IEnumerable<int> combination in combinations)
            {
                actual.Add(combination.ToList());
            }

            actual.ForEach(pair => Trace.WriteLine(string.Join(", ", pair)));
            for (int i = 0; i < actual.Count; i++)
            {
                CollectionAssert.AreEqual(expected[i], actual[i]);
            }

        }
    }
}