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
                int observedRooms = 12; // ToDo: Let user decide

                DungeonSampler(dungeonModel, (FeatureType.NumRooms, observedRooms)); // For this experiment we are observing only the total number of rooms according to user specification.

                SpaceGraph graph = new SpaceGraph();

                ///
                /// ~ Building the Space graph for the map generator ~
                /// 
                for (int i = 0; i < observedRooms; i++)
                {
                    graph.CreateNode(i);
                }

                // Create the critical path and set the distance for all nodes on it to 0
                for (int pid = 0; pid < dungeonModel.Value(FeatureType.CriticalPathLength); pid++)
                {
                    int child = (pid != dungeonModel.Value(FeatureType.CriticalPathLength) - 1) ? pid + 1 : graph.Goal.Id; // The critical path is comprised of consequtive rooms from the start + the last one (goal)
                    graph.Node(pid).CPDistance = 0;
                    graph.Node(child).CPDistance = 0;
                    graph.Node(pid).Depth = pid;
                    graph.Node(child).Depth = pid + 1;
                    graph.Connect(pid, child);
                }

                // Sample room params and set the node constraints: Depth, MaxNeighbours, CPDistance




                // Transform the basis dungeon in a per-room fashion
                foreach (Node parent in graph.AllNodes)
                {
                    if (parent.CPDistance != null && parent.CPDistance == 0) //this room is already on the critical path so CPDist and Depth are set
                    {
                        SpaceModel roomModel = RoomSampler(dungeonModel);
                        parent.MaxNeighbours = (int)dungeonModel.Value(FeatureType.NumNeighbours);
                    }
                    else
                    {
                        SpaceModel roomModel = RoomSampler(dungeonModel);
                        parent.Depth = (int)dungeonModel.Value(FeatureType.Depth);
                        parent.MaxNeighbours = (int)dungeonModel.Value(FeatureType.NumNeighbours);
                        parent.CPDistance = (int)dungeonModel.Value(FeatureType.CriticalPathDistance);
                    }
                }



                // Compose a list of all nodes that still don't have their full neighbours reached
                // randomly assign neighbours and remove any fully connected node from the list
                // repeat untill all have been assigned
                // the data handles some constraints implicitly (e.g. data guarantees that there will be no room with MaxNeighbours == 0)

                
                
                List<Node> unconnected = graph.AllNodes.FindAll(node => node.MaxNeighbours - node.Edges.Count > 0);

                while (unconnected.Count != 0)
                {
                    Node parent = unconnected[rand.Next(0, unconnected.Count)];
                    int unconnectedNeighbours = parent.MaxNeighbours.Value - parent.Edges.Count;
                    int retries = 10;
                    while (unconnectedNeighbours != 0 && retries >= 0)
                    {
                        // if connecting A:B does not invalidate the constraints => connect them
                        // hard constraints, maintain :: cpLength | A neighbours & depth | B neighbours & depth
                        // soft constraints, maintain :: doors    | A cpDistance         | B cpDistance
                        Node child = unconnected[rand.Next(0, unconnected.Count)];

                        if (ValidCPLength(graph, parent, child)) //check if graph is planar
                        {
                            if (ValidNeighboursPostInc(parent) && ValidNeighboursPostInc(child))
                            {
                                graph.Connect(parent, child);
                                unconnectedNeighbours--;
                            }
                            else retries--;
                        }
                        else
                        {
                            Debug.WriteLine($"Did not find any match for [parent, child] :: [{parent.Id},{child.Id}], retring {retries} more times...");
                            retries--;
                        }
                    }
                    if (unconnectedNeighbours == 0)
                    {
                        unconnected.Remove(parent);
                    }
                }

                //for (int par = 0; par < rooms; par++)
                //{
                //    Node parent = graph.Node(par);
                //    int unconnectedNeighbours = parent.MaxNeighbours - parent.Edges.Count;
                //    int retries = 10;
                //    while (unconnectedNeighbours != 0 && retries >= 0)
                //    {
                //        // if connecting A:B does not invalidate the constraints => connect them
                //        // hard constraints, maintain :: cpLength | A neighbours & depth | B neighbours & depth
                //        // soft constraints, maintain :: doors    | A cpDistance         | B cpDistance
                //        Node child = graph.Node(rand.Next(0, rooms));

                //        SpaceGraph mutated = ValidCPLength(graph, parent, child);
                //        if (mutated != null && mutated.isPlanar)
                //        {
                //            var mutParent = mutated.Node(par);
                //            var mutChild = mutated.Node(child.Id);
                //            if (ValidNeighbours(mutParent, mutParent.MaxNeighbours) && ValidNeighbours(mutChild, mutChild.MaxNeighbours))
                //            {
                //                graph.Connect(mutParent, mutChild);
                //                unconnectedNeighbours--;
                //            }
                //            else retries--;
                //        }
                //        else
                //        {
                //            Debug.WriteLine($"Did not find any match for [parent, child] :: [{parent.Id},{child.Id}], retring {retries} more times...");
                //            retries--;
                //        }
                //    }
                //}


                // Validate if graph is complete.
                Debug.WriteLine($"Is graph complete? {graph.isComplete}");
                Debug.WriteLine($"Is graph planar? {graph.isPlanar}");

                Debug.Write(graph.ToString());
                return graph;
            }
            finally
            {
                //clean-up
                dungeonBNLoader.close();
            }
        }
        #region Graph transforms
        /// <summary>
        /// Randomly select nodes and connect them until no dangling nodes remain
        /// Dummy logic, does not maintain invariants
        /// </summary>
        private static SpaceGraph RandomMapper(SpaceGraph graph)
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
        #endregion

        #region Samplers
        /// <summary>
        /// Sample dungeon parameters given a set of observations.
        /// </summary>
        /// <param name="dungeonModel">Inference model</param>
        /// <param name="observations">Our beliefs/observations</param>
        private static void DungeonSampler(SpaceModel dungeonModel, params (FeatureType, int)[] observations)
        {
            dungeonModel.SetObservations(true, observations);
            _ = dungeonModel.Sample();
            //Get the global (Dungeon) parameters
            int rooms = (int)dungeonModel.Value(FeatureType.NumRooms);               //Hard constraint
            int cpLength = (int)dungeonModel.Value(FeatureType.CriticalPathLength);  //Hard constraint
            int doors = (int)dungeonModel.Value(FeatureType.NumDoors);               //Soft constraint

            Debug.WriteLine($"Dungeon Sampled: [{rooms},{cpLength},{doors}]");
        }
        /// <summary>
        /// Sample room parameters from our SpaceModel and assign them to each node in the physical SpaceGraph
        /// Must be run after evidence for the global dungeon parameters is set
        /// </summary>
        /// <param name="dungeonModel">Inference model</param>
        /// <param name="graph">Dungeon graph/layout</param>
        private static SpaceModel RoomSampler(SpaceModel dungeonModel)
        {
            int rooms = (int)dungeonModel.Value(FeatureType.NumDoors);
            int cpLength = (int)dungeonModel.Value(FeatureType.CriticalPathLength);
            int doors = (int)dungeonModel.Value(FeatureType.NumDoors);

            // Transform the basis dungeon in a per-room fashion
            //set global observations and maintain them :: clear -> sample -> reset
            //make sure you don't invalidate the hard constraints and log when the soft ones have to be adjusted

            dungeonModel.SetObservations(true,
                (FeatureType.NumRooms, rooms),
                (FeatureType.CriticalPathLength, cpLength),
                (FeatureType.NumDoors, doors));

            //ToDo: Get room parameters and do room allocation

            _ = dungeonModel.Sample();

            int depth = (int)dungeonModel.Value(FeatureType.Depth);                     //Hard constraint
            int maxNeighbours = (int)dungeonModel.Value(FeatureType.NumNeighbours);     //Hard constraint
            int cpDistance = (int)dungeonModel.Value(FeatureType.CriticalPathDistance); //Soft constraint therefore if we sample 0, we can retry

            if (depth == 0 || cpDistance == 0) //retry if we've sampled an entrance room or a room on the critical path, because they are already set
            {
                Debug.WriteLine($"Invalid room: [{cpDistance},{depth},{maxNeighbours}], retrying...");
                return RoomSampler(dungeonModel);
            }
            else
            {
                Debug.WriteLine($"Valid room: [{cpDistance},{depth},{maxNeighbours}]");
                return dungeonModel;
            }



        }
        #endregion

        #region Constraints
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
        /// Assume we've added an edge to A.
        /// Validate if A is still within capacity.
        /// </summary>
        /// <param name="A">node</param>
        /// <returns>If A has not exceeded its neighbour capacity</returns>
        private bool ValidNeighboursPostInc(Node A) => (A.Edges.Count + 1) <= A.MaxNeighbours;
        #endregion


    }
}
