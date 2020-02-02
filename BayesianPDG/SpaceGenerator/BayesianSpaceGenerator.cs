using BayesianPDG.SpaceGenerator.Space;
using Netica;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BayesianPDG.SpaceGenerator
{
    class BayesianSpaceGenerator
    {
        /// <summary>
        /// Global reference to the Netica COM
        /// </summary>
        public static Application NeticaApp = new Application();
        public SpaceGraph RunInference(int? seed = null)
        {
            DAGLoader dungeonBNLoader = new DAGLoader("Resources\\BNetworks\\EMNet.neta");
            SpaceModel dungeonModel = new SpaceModel(dungeonBNLoader);
            Random rand = (seed == null) ? new Random() : new Random(seed.Value);
            try
            {
                //Configure observations, i.e. how many rooms in the dungeon
                int observedRooms = 6; // ToDo: Let user decide
                dungeonModel.Observe(FeatureType.NumRooms, observedRooms);

                _ = dungeonModel.Sample();
                //Get the global (Dungeon) parameters
                int rooms = (int)dungeonModel.Value(FeatureType.NumRooms);               //Hard constraint
                int cpLength = (int)dungeonModel.Value(FeatureType.CriticalPathLength);  //Hard constraint
                int doors = (int)dungeonModel.Value(FeatureType.NumDoors);               //Soft constraint

                Debug.WriteLine($"Dungeon Sampled: [{rooms},{cpLength},{doors}]");

                SpaceGraph graph = new SpaceGraph();

                ///
                /// ~ Building the Space graph for the map generator ~
                /// 
                for (int i = 0; i < rooms; i++)
                {
                    graph.CreateNode(i);
                }

                // Create the critical path and set the distance for all nodes on it to 0
                for (int parent = 0; parent < cpLength; parent++)
                {
                    int child = (parent != cpLength - 1) ? parent + 1 : graph.Goal.Id; // The critical path is comprised of consequtive rooms from the start + the last one (goal)
                    graph.Node(parent).CPDistance = 0;
                    graph.Node(child).CPDistance = 0;
                    graph.Connect(parent, child);
                }

                // Transform the basis dungeon in a per-room fashion
                for (int par = 0; par < rooms; par++)
                {
                    //set global observations and maintain them :: clear -> sample -> reset
                    //make sure you don't invalidate the hard constraints and log when the soft ones have to be adjusted

                    dungeonModel.ClearObservations();
                    dungeonModel.Observe(FeatureType.NumRooms, observedRooms);
                    dungeonModel.Observe(FeatureType.CriticalPathLength, cpLength);
                    dungeonModel.Observe(FeatureType.NumDoors, doors);

                    //ToDo: Get room parameters and do room allocation
                    Node parent = graph.Node(par);

                    if (parent.CPDistance == 0) // if its on the critical path
                    {
                        dungeonModel.Observe(FeatureType.CriticalPathDistance, 0);
                        if (parent.Id == 0) // if its also the entrance
                        {
                            dungeonModel.Observe(FeatureType.Depth, 0);
                        }
                    }

                    _ = dungeonModel.Sample();

                    parent.Depth = (int)dungeonModel.Value(FeatureType.Depth);                     //Hard constraint
                    parent.Neighbours = (int)dungeonModel.Value(FeatureType.NumNeighbours);        //Hard constraint
                    parent.CPDistance = (int)dungeonModel.Value(FeatureType.CriticalPathDistance); //Soft constraint

                    Debug.WriteLine($"Rooms Sampled: [{graph.Node(par).Depth},{graph.Node(par).Neighbours},{graph.Node(par).CPDistance}]");

                }
                for (int par = 0; par < rooms; par++)
                {
                    Node parent = graph.Node(par);
                    int unconnectedNeighbours = parent.Neighbours - parent.Edges.Count;
                    int retries = 10;
                    while (unconnectedNeighbours != 0 && retries >= 0)
                    {
                        // if connecting A:B does not invalidate the constraints => connect them
                        // hard constraints, maintain :: cpLength | A neighbours & depth | B neighbours & depth
                        // soft constraints, maintain :: doors    | A cpDistance         | B cpDistance
                        Node child = graph.Node(rand.Next(1, rooms-1));
                        if (ValidCPLength(graph, parent, child) && ValidNeighbours(parent, parent.Neighbours) && ValidNeighbours(child, child.Neighbours))
                        {
                            graph.Connect(parent, child);
                            unconnectedNeighbours--;
                        }
                        else
                        {
                            Debug.WriteLine($"Did not find any match for [parent, child] :: [{parent.Id},{child.Id}], retring {retries} more times...");
                            retries--;
                        }
                    }
                }

                // RandomConnect(graph);

                // Validate if graph is complete.
                Debug.WriteLine($"Is graph complete? {graph.isComplete}");

                Debug.Write(graph.ToString());
                return graph;
            }
            finally
            {
                //clean-up
                dungeonBNLoader.close();
            }
        }
        /// <summary>
        /// Randomly select nodes and connect them until no dangling nodes remain
        /// Dummy logic, does not maintain invariants
        /// </summary>
        private static SpaceGraph RandomConnect(SpaceGraph graph)
        {
            int rooms = graph.AllNodes.Count;
            while (!graph.isComplete)
            {
                Random random = new Random();
                int randParent = random.Next(1, rooms - 1);
                int randChild = random.Next(1, rooms - 1);
                graph.Connect(randParent, randChild);
            }
            return graph;
        }

        /// <summary>
        /// Validate if adding node A to node B will break the invariant
        /// i.e. if it will change the critical path length
        /// </summary>
        /// <param name="graph">Dungeon topology graph</param>
        /// <param name="A">parent node</param>
        /// <param name="B">child node</param>
        /// <returns>If adding A:B is a valid operation</returns>
        private bool ValidCPLength(SpaceGraph graph, Node A, Node B)
        {
            int originalCPLength = graph.CriticalPath.Count;
            SpaceGraph temp = new SpaceGraph(graph);
            temp.Connect(A, B);

            return temp.CriticalPath.Count == originalCPLength;
        }

        /// <summary>
        /// Validate if a node A still has room for more neighbours
        /// </summary>
        /// <param name="A">node</param>
        /// <returns>If A has not exceeded its neighbour capacity</returns>
        private bool ValidNeighbours(Node A, int maxNe) => A.Edges.Count < maxNe;

    }
}
