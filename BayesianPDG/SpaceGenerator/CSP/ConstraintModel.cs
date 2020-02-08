using BayesianPDG.SpaceGenerator.Space;
using Decider.Csp.BaseTypes;
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
        private IList<VariableInteger> Variables { get; set; }
        private IList<IConstraint> Constraints { get; set; }
        public IState<int> State { get; private set; }
        public IList<IDictionary<string, IVariable<int>>> Solutions { get; private set; }

        public ConstraintModel(SpaceGraph graph)
        {
            // Each node should choose the subset of those edges that fit the constraints

            // Create the variables.

            // The graph should already contain initialised Values with respect to the samples
            // int[][,] conMatrix = new int[graph.AllNodes.Count][,]; //jagged array that will containt int[] for all combinations
            //List<List<List<int>>> con2DList = new List<List<List<int>>>();
            //graph.AllNodes.ForEach(room =>
            //{
            //    List<List<int>> row = new List<List<int>>();
            //    foreach (List<Node> valueSet in room.Values)
            //    {
            //        List<int> set = new List<int>();
            //        foreach (Node connection in valueSet)
            //        {
            //            set.Add(connection.Id);
            //        }
            //        row.Add(set);
            //    }
            //    con2DList.Add(row);
            //});

            Variables = new VariableInteger[graph.AllNodes.Count];

            graph.AllNodes.ForEach(node => Variables[node.Id] = new VariableInteger($"{node.Id}", 0, graph.AllNodes.Count - 1));


            /// Constraints
            /// =======================================================================================================
            /// Cardinality :: Values.Count == MaxNeighbours, enforced by default in choosing the Values
            /// Ordering    :: Node[Entrance] > Node[1] ... Node[Goal] && Node[Goal] < Node[Goal -1] ... Node[Entrance]
            /// Relationship:: Node[i]::Node[j] must not change the CPLength for any i and j
            /// Relationship:: Graph must be fully connected, isReachable(Node[Entrance],Node[i]) for any i
            /// Functional  :: Graph must be planar. Euler's method must hold as invariant
            /// 

            //Not allowed to be connected to oneself
            Constraints = new List<IConstraint>();
            foreach (VariableInteger con in Variables)
            {
                Constraints.Add(new ConstraintInteger(con != int.Parse(con.Name)));
            }

            //Done setting up the model. Now we just have to surch over solutions
            Search();
            PrintRandomSolution(1);
        }

        public void Search()
        {
            State = new StateInteger(Variables, Constraints);
            State.StartSearch(out StateOperationResult searchResult, out IList<IDictionary<string, IVariable<int>>> solutions);
            Solutions = solutions;
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
            Random rng = (seed != null)? new Random(seed.Value): new Random();

            IDictionary<string, IVariable<int>> solution = Solutions[rng.Next(0, State.NumberOfSolutions - 1)];
            for (int i = 0; i < solution.Count; i++)
            {
                Debug.WriteLine($"Node[{solution.Keys.ToArray()[i]}]:{solution.Values.ToArray()[i]}");
            }
        }
    }
}