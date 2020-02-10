using BayesianPDG.SpaceGenerator.Space;
using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BayesianPDG.SpaceGenerator.CSP
{
    public class ConstraintModel
    {
        //      private IList<VariableInteger> Variables { get; set; }
        //private IList<VariableInteger[][]> Variables { get; set; }
        private IList<VariableInteger[]> Variables { get; set; }
        private IList<IConstraint> Constraints { get; set; }
        public IState<int> State { get; private set; }
        public IList<IDictionary<string, IVariable<int>>> Solutions { get; private set; }

        public ConstraintModel(SpaceGraph graph)
        {
            // Each node should choose the subset of those edges that fit the constraints
            // Create the variables.
            //   Variables = new VariableInteger[graph.AllNodes.Count];
            //   graph.AllNodes.ForEach(node => Variables[node.Id] = new VariableInteger($"{node.Id}", 0, graph.AllNodes.Count - 1));

            //Variables = new List<VariableInteger[][]>(graph.AllNodes.Count);
            //graph.AllNodes.ForEach(node =>
            //{
            //    Variables.Add(new VariableInteger[node.Values.Count][]);
            //    foreach (List<Node> valueSet in node.Values) 
            //    {
            //        int[] ids = valueSet.Select(x => x.Id).ToArray();
            //        Variables[node.Id][node.Id].Append<VariableInteger>(new VariableInteger($"{node.Id}:[{string.Join(",",ids)}]", ids));
            //    }
            //});

            Variables = new List<VariableInteger[]>(graph.AllNodes.Count);
            graph.AllNodes.ForEach(node =>
            {
            Variables.Add(new VariableInteger[node.MaxNeighbours.Value]);
            for (int i = 0; i < Variables[node.Id].Length; i++)
            {
                Variables[node.Id][i] = new VariableInteger($"{node.Id}:{i}", 0, graph.AllNodes.Count - 1);
                }
            });


            foreach (var val in Variables)
            {
                Debug.WriteLine($"CONSTRAINT VARIBLES {string.Join(",", val.Select(x => x).ToList())}");
            }

            /// Constraints
            /// =======================================================================================================
            /// Cardinality :: Values.Count == MaxNeighbours, enforced by default in choosing the Values
            /// Ordering    :: Node[Entrance] > Node[1] ... Node[Goal] && Node[Goal] < Node[Goal -1] ... Node[Entrance]
            /// Relationship:: Node[i]::Node[j] must not change the CPLength for any i and j
            /// Relationship:: Graph must be fully connected, isReachable(Node[Entrance],Node[i]) for any i
            /// Functional  :: Graph must be planar. Euler's method must hold as invariant
            /// 
            Constraints = new List<IConstraint>();


            //Cannot be connected to onesself
            foreach (VariableInteger[] set in Variables)
            {
                foreach (VariableInteger con in set)
                {
                    Constraints.Add(new ConstraintInteger(con != int.Parse(con.Name[0].ToString())));
                }
            }

            //cannot be connected to the same one twice
            foreach (VariableInteger[] set in Variables)
            {
                Constraints.Add(new AllDifferentInteger(set));
            }

            int[][] adjMatrix = new int[Variables.Count][];
            for (int row = 0; row < Variables.Count; row++)
            {
                adjMatrix[row] = new int[Variables.Count];
                for (int col = 0; col < Variables.Count; col++)
                {
                    adjMatrix[row][col] = 0;
                }
            }

            ConstrainedArray[] consAdjMatrix = new ConstrainedArray[Variables.Count * Variables.Count];

            for (int row = 0; row < Variables.Count; row++)
            {
                for (int col = 0; col < Variables.Count; col++)
                {
                    adjMatrix[row][col] = 1;
                    adjMatrix[col][row] = 1;
                    consAdjMatrix[row] = new ConstrainedArray(adjMatrix[row]);
                    consAdjMatrix[col] = new ConstrainedArray(adjMatrix[col]);
                    adjMatrix[row][col] = 0;
                    adjMatrix[col][row] = 0;
                }
            }



            //Symmetric connections A->B means that B->A
            for (int row = 0; row < Variables.Count(); row++)
            {
                for (int col = 0; col < Variables[row].Count(); col++)
                {
                    var constant = new VariableInteger("const", 0, Variables.Count());
                    Constraints.Add(new ConstraintInteger(consAdjMatrix[row][col] == constant & consAdjMatrix[col][row] == constant));
                }
            }

            // Entrance must be before the others
            //foreach(VariableInteger[] set in Variables)
            //{
            //    if (!set[0].Name[0].Equals('0')) 
            //    {
            //      foreach(VariableInteger con in set)
            //        {
            //            Constraints.Add(new ConstraintInteger());
            //        }
            //    }
            //}



            //Done setting up the model. Now we just have to surch over solutions
            Search();
            PrintRandomSolution(1);
        }

        public void Search()
        {
            State = new StateInteger(Variables.SelectMany(x => x.Select(y => y)), Constraints);
            State.StartSearch(out StateOperationResult searchResult, out IList<IDictionary<string, IVariable<int>>> solutions);
            Solutions = solutions;
            if (Solutions.Count == 0) throw new Exception("No solutions found");
        }

        public void PrintAllSolutions()
        {
            foreach (IDictionary<string, IVariable<int>> solution in Solutions)
            {
                var keys = solution.SelectMany(x => x.Key).ToList();
                var values = solution.Select(x => x.Value).ToList();

                for (int i = 0; i < keys.Count; i++)
                {
                    Debug.WriteLine($"{keys[i]}{values[i]}");
                }
            }

            Console.WriteLine("Runtime:\t{0}", State.Runtime);
            Console.WriteLine("Backtracks:\t{0}", State.Backtracks);
            Console.WriteLine("Solutions:\t{0}", State.NumberOfSolutions);
        }

        public void PrintRandomSolution(int? seed)
        {
            Random rng = (seed != null) ? new Random(seed.Value) : new Random();

            IDictionary<string, IVariable<int>> solution = Solutions[rng.Next(0, State.NumberOfSolutions - 1)];
            for (int i = 0; i < solution.Count; i++)
            {
                Console.WriteLine($"Node[{solution.Keys.ToArray()[i]}]:{solution.Values.ToArray()[i]}");
            }
        }
    }
}