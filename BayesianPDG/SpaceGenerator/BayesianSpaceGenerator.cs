using BayesianPDG.SpaceGenerator.Space;
using Netica;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BayesianPDG.SpaceGenerator
{
    public class BayesianSpaceGenerator
    {
        /// <summary>
        /// Global reference to the Netica COM
        /// </summary>
        public static Application NeticaApp = new Application();
        private SpaceModel DungeonModel;

        public SpaceGraph RunInference(int? seed = null)
        {
            DAGLoader dungeonBNLoader = new DAGLoader("Resources\\BNetworks\\EMNet.neta");
            DungeonModel = new SpaceModel(dungeonBNLoader);
            Random rand = (seed == null) ? new Random() : new Random(seed.Value); //Maintain the same RNG throuought for consistency
            try
            {
                //Configure observations, i.e. how many rooms in the dungeon
                SpaceGraph graph = new SpaceGraph();

                int observedRooms = 6; // ToDo: Let user decide

                DungeonSampler((FeatureType.NumRooms, observedRooms)); // For this experiment we are observing only the total number of rooms according to user specification.

                ///
                /// ~ Building the Space graph for the map generator ~
                /// 
                for (int i = 0; i < observedRooms; i++)
                {
                    graph.CreateNode(i);
                }

                // Create the critical path and set the distance for all nodes on it to 0
                graph = CriticalPathMapper(graph, (int) DungeonModel.Value(FeatureType.CriticalPathLength));

                // Sample room params and set the node constraints: Depth, MaxNeighbours, CPDistance
                foreach (Node parent in graph.AllNodes)
                {
                    RoomSampler(parent);
                }

                Debug.WriteLine("=== Final set of rooms sampled ===");
                Debug.WriteLine("=== [cpDistance, depth, maxNe] ===");
                graph.AllNodes.ForEach(node => Debug.WriteLine($"[{node.CPDistance},{node.Depth},{node.MaxNeighbours}]"));


                // Transform the basis dungeon in a per-room fashion
                // Compose a list of all nodes that still don't have their full neighbours reached
                // randomly assign neighbours and remove any fully connected node from the list
                // repeat untill all have been assigned
                // the data handles some constraints implicitly (e.g. data guarantees that there will be no room with MaxNeighbours == 0)
                graph = NeighbourMapper(graph, rand);
                

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

        public SpaceGraph CriticalPathMapper(SpaceGraph graph, int CPLength)
        {
            for (int pid = 0; pid < CPLength - 1; pid++)
            {
                int child = (pid != CPLength - 2) ? pid + 1 : graph.Goal.Id; // The critical path is comprised of consequtive rooms from the start + the last one (goal)
                graph.Node(child).CPDistance = 0;
                graph.Node(child).Depth = (child == graph.Goal.Id)? CPLength - 1 : pid + 1;
                graph.Connect(pid, child);
            }
            return graph;
        }

        public SpaceGraph NeighbourMapper(SpaceGraph graph, Random rng)
        {
            List<Node> unconnected = graph.AllNodes.FindAll(node => node.MaxNeighbours - node.Edges.Count > 0);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            while (unconnected.Count != 0 && stopWatch.Elapsed.TotalMinutes < 0.5)
            {
                //Debug.WriteLine($"Unconnected nodes left {unconnected.Count}");
                Node parent = unconnected[rng.Next(0, unconnected.Count)];
                int unconnectedNeighbours = parent.MaxNeighbours.Value - parent.Edges.Count;
                int retries = 10;
                while (unconnectedNeighbours >= 0 && retries >= 0)
                {
                    // if connecting A:B does not invalidate the constraints => connect them
                    // hard constraints, maintain :: cpLength | A neighbours & depth | B neighbours & depth
                    // soft constraints, maintain :: doors    | A cpDistance         | B cpDistance

                    List<Node> temp = new List<Node>(unconnected); //avoid duplicates
                    temp.Remove(parent);
                    if (temp.Count == 0) break;

                    Node child = temp[rng.Next(0, temp.Count)];

                    if (ValidCPLength(graph, parent, child))
                    { //check if graph is planar
                        var validNeigbours = (parentIsValid: ValidNeighboursPostInc(parent), childIsValid: ValidNeighboursPostInc(child));
                        if (!validNeigbours.parentIsValid)
                        {
                            //     Debug.WriteLine($"Invalid Parent {parent.Id}: [{parent.Edges.Count}]<=[{parent.MaxNeighbours}]");
                            retries--;
                        }
                        else if (!validNeigbours.childIsValid)
                        {
                            //      Debug.WriteLine($"Child {child.Id}: [{child.Edges.Count}]<=[{child.MaxNeighbours}]");
                            retries--;
                        }
                        else
                        {
                            //      Debug.WriteLine($"Connecting valid {parent.Id}::{child.Id} ...");
                            graph.Connect(parent, child);
                            unconnectedNeighbours--;
                        }
                    }
                    else
                    {
                        // Debug.WriteLine($"Did not find any match for [parent, child] :: [{parent.Id},{child.Id}], retring {retries} more times...");
                        retries--;
                    }
                }
                if (unconnectedNeighbours == 0)
                {
                    Debug.WriteLine($"Finished [{parent.Id}], removing node...");
                    unconnected.Remove(parent);
                }
            }
            return graph;
        }
        #endregion

        #region Samplers
        /// <summary>
        /// Sample dungeon parameters given a set of observations.
        /// </summary>
        /// <param name="DungeonModel">Inference model</param>
        /// <param name="observations">Our beliefs/observations</param>
        public void DungeonSampler( params (FeatureType, int)[] observations)
        {
            DungeonModel.SetObservations(true, observations);
            _ = DungeonModel.Sample();
            //Get the global (Dungeon) parameters
            int rooms = (int)DungeonModel.Value(FeatureType.NumRooms);               //Hard constraint
            int cpLength = (int)DungeonModel.Value(FeatureType.CriticalPathLength);  //Hard constraint
            int doors = (int)DungeonModel.Value(FeatureType.NumDoors);               //Soft constraint

            Debug.WriteLine($"Dungeon Sampled: [{rooms},{cpLength},{doors}]");
        }
        /// <summary>
        /// Sample room parameters from our SpaceModel and assign them to a node
        /// Must be run after evidence for the global dungeon parameters is set
        /// </summary>
        /// <param name="DungeonModel">Inference model</param>
        /// <param name="node">room</param>
        public Node RoomSampler(Node room)
        {
            //Get dungeon params
            int rooms = (int)DungeonModel.Value(FeatureType.NumDoors);
            int cpLength = (int)DungeonModel.Value(FeatureType.CriticalPathLength);
            int doors = (int)DungeonModel.Value(FeatureType.NumDoors);

            //1 set global observations and maintain them :: clear -> sample -> reset
            //2 set any known room observations (i.e. depth and cpDistance for Entrance node and cpDistance for rooms on the critical path) 
            //make sure you don't invalidate the hard constraints and log when the soft ones have to be adjusted

            DungeonModel.SetObservations(true,
                (FeatureType.NumRooms, rooms),
                (FeatureType.CriticalPathLength, cpLength),
                (FeatureType.NumDoors, doors));

            if (room.CPDistance == 0 && room.Depth != null)  //room on critical path
            {
                DungeonModel.Observe(FeatureType.CriticalPathDistance, 0);
                DungeonModel.Observe(FeatureType.Depth, room.Depth.Value);

                _ = DungeonModel.Sample();

                room.MaxNeighbours = (int)DungeonModel.Value(FeatureType.NumNeighbours);    //Hard constraint
            }
            else // room not on critical path
            {
                _ = DungeonModel.Sample();

                int depth = (int)DungeonModel.Value(FeatureType.Depth);                     //Soft constraint
                int maxNeigh = (int)DungeonModel.Value(FeatureType.NumNeighbours);          //Hard constraint
                int cpDistance = (int)DungeonModel.Value(FeatureType.CriticalPathDistance); //Hard constraint

                if (depth != 0 && cpDistance != 0)
                {
                    room.Depth = depth;
                    room.MaxNeighbours = maxNeigh;
                    room.CPDistance = cpDistance;
                }
                else return RoomSampler(room);
            }
            

            return room;
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
        public bool ValidCPLength(SpaceGraph graph, Node A, Node B)
        {
            int originalCPLength = graph.CriticalPath.Count;
            graph.Connect(A, B);
            bool isCPValid = graph.CriticalPath.Count == originalCPLength;
            graph.Disconnect(A, B);
            return isCPValid;
        }

        /// <summary>
        /// Assume we've added an edge to A.
        /// Validate if A is still within capacity.
        /// </summary>
        /// <param name="A">node</param>
        /// <returns>If A has not exceeded its neighbour capacity</returns>
        private bool ValidNeighboursPostInc(Node A) => A.Edges.Count < A.MaxNeighbours;
        #endregion


    }
}
