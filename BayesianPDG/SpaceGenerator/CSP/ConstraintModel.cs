using BayesianPDG.SpaceGenerator.Space;
using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BayesianPDG.SpaceGenerator.CSP
{
    public class ConstraintModel
    {
        public ConstraintModel(SpaceGraph graph)
        {

            // Instantiate the solver
            Solver solver = new Solver("CSDungeon");

            // Create the variables.
            const long numVals = 3;
            graph.AllNodes.ForEach(room =>
            {
               int[] potentialConnections = room.Values.SelectMany(x => x).Select(y => y.Id).ToArray();
                solver.MakeIntVarArray(potentialConnections.Length, potentialConnections, "Connections");
            });

            IntVar x = solver.MakeIntVar(0, numVals - 1, "x");
            IntVar y = solver.MakeIntVar(0, numVals - 1, "y");
            IntVar z = solver.MakeIntVar(0, numVals - 1, "z");

            // Constraint 0: x != y..
            solver.Add(solver.MakeAllDifferent(new IntVar[] { x, y }));
            Console.WriteLine($"Number of constraints: {solver.Constraints()}");

            // Solve the problem.
            DecisionBuilder db = solver.MakePhase(
                new IntVar[] { x, y, z },
                Solver.CHOOSE_FIRST_UNBOUND,
                Solver.ASSIGN_MIN_VALUE);

            // Print solution on console.
            int count = 0;
            solver.NewSearch(db);
            while (solver.NextSolution())
            {
                ++count;
                Console.WriteLine($"Solution: {count}\n x={x.Value()} y={y.Value()} z={z.Value()}");
            }
            solver.EndSearch();
            Console.WriteLine($"Number of solutions found: {solver.Solutions()}");

            // Metrics
            Console.WriteLine("Advanced usage:");
            Console.WriteLine($"Problem solved in {solver.WallTime()}ms");
            Console.WriteLine($"Memory usage: {Solver.MemoryUsage()}bytes");
        }
    }
}
