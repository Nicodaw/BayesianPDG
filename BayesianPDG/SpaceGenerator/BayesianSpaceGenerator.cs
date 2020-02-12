using BayesianPDG.SpaceGenerator.CSP;
using BayesianPDG.SpaceGenerator.Space;
using BayesianPDG.Utils;
using Netica;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BayesianPDG.SpaceGenerator
{
    public class BayesianSpaceGenerator
    {
        /// <summary>
        /// Global reference to the Netica COM
        /// </summary>
        public static Application NeticaApp = new Application();

        /// <summary>
        /// Maintain the same RNG throuought for consistency
        /// </summary>
        private Random RNG;

        /// <summary>
        /// Model reference
        /// </summary>
        private SpaceModel DungeonModel;

        /// <summary>
        /// Dungeon graph reference
        /// </summary>
        private SpaceGraph DungeonGraph;

        #region Dungeon Parameters
        private int Rooms;
        private int CPLength;
        private int Doors;
        #endregion

        public SpaceGraph RunInference(int? seed = null)
        {
            DAGLoader dungeonBNLoader = new DAGLoader("Resources\\BNetworks\\EMNet.neta");
            DungeonModel = new SpaceModel(dungeonBNLoader);
            RNG = (seed == null) ? new Random() : new Random(seed.Value);
            try
            {
                //Configure observations, i.e. how many rooms in the dungeon
                DungeonGraph = new SpaceGraph();

                int observedRooms = 6; // ToDo: Let user decide

                DungeonSampler((FeatureType.NumRooms, observedRooms)); // For this experiment we are observing only the total number of rooms according to user specification.

                ///
                /// ~ Building the Space graph for the map generator ~
                /// 
                for (int i = 0; i < observedRooms; i++)
                {
                    DungeonGraph.CreateNode(i);
                }

                // Create the critical path and set the distance for all nodes on it to 0
                DungeonGraph = CriticalPathMapper(DungeonGraph, (int)DungeonModel.Value(FeatureType.CriticalPathLength));

                // Sample room params and set the node constraints: Depth, MaxNeighbours, CPDistance
                foreach (Node parent in DungeonGraph.AllNodes)
                {
                    RoomSampler(parent);
                }

                Debug.WriteLine("=== Final set of rooms sampled ===");
                Debug.WriteLine("=== [cpDistance, depth, maxNe] ===");
                DungeonGraph.AllNodes.ForEach(node => Debug.WriteLine($"[{node.CPDistance},{node.Depth},{node.MaxNeighbours}]"));

                // Initialise the potential connections for formulating the CSP
                // We're externally enforcing the cardinality constraint by instantiating the Values as a K combinatorial set of the N rooms ( K = MaxNeighbours for each node.Values)
                DungeonGraph.ReducePotentialValues();


                Debug.WriteLine("Potential room connections before CP mapping");
                DungeonGraph.AllNodes.ForEach(node => Debug.WriteLine(node.PrintConnections()));

                // Transform the basis dungeon in a per-room fashion
                // Compose a list of all nodes that still don't have their full neighbours reached
                // randomly assign neighbours and remove any fully connected node from the list
                // repeat untill all have been assigned
                // the data handles some constraints implicitly (e.g. data guarantees that there will be no room with MaxNeighbours == 0)
                //DungeonGraph = NeighbourMapper(DungeonGraph);
                Task task = new Task(Map);
                try
                {
                    int secondsTimeout = 5;
                    task.Start();
                    task.Wait(secondsTimeout * 1000);
                }
                finally
                {
                    Debug.WriteLine($"Is mapping successful? {task.IsCompleted}");
                }

                // Validate if graph is complete.
                Debug.WriteLine($"Is DungeonGraph complete? {DungeonGraph.isComplete}");
                Debug.WriteLine($"Is DungeonGraph planar? {DungeonGraph.isPlanar}");
                List<Node> unconnected = DungeonGraph.AllNodes.FindAll(node => node.MaxNeighbours - node.Edges.Count > 0);
                List<Node> connected = DungeonGraph.AllNodes.FindAll(node => node.MaxNeighbours - node.Edges.Count == 0);
                string unc = string.Join(", ", unconnected.Select(x => x.Id).ToArray());
                string con = string.Join(", ", connected.Select(x => x.Id).ToArray());
                Debug.WriteLine($"List of Unconected nodes {unc}");
                Debug.WriteLine($"List of Connected nodes {con}");
                Debug.Write(DungeonGraph.ToString());
                return DungeonGraph;
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
                graph.Node(child).Depth = (child == graph.Goal.Id) ? CPLength - 1 : pid + 1;
                graph.Connect(pid, child);
            }
            return graph;
        }

        private SpaceGraph MapOne(Node A, Node B)
        {
            if (DungeonGraph.ValidCPLength(A, B))
            {
                var validNeigbours = (parentIsValid: DungeonGraph.ValidNeighboursPostInc(A), childIsValid: DungeonGraph.ValidNeighboursPostInc(B));
                if (!validNeigbours.parentIsValid || !validNeigbours.childIsValid) return null;
                else DungeonGraph.Connect(A, B);
            }
            return null;
        }

        private void Map()
        {
            foreach (Node node in DungeonGraph.AllNodes.ToList())
            {
                //Thread thread = new Thread(MapOne);
                //thread.Start();
                try 
                {
                    MapOne();
                }
                catch (StackOverflowException so)
                {
                    throw new StackOverflowException(so.StackTrace);
                }
            }
        }

        private void MapOne()
        {
            Stack<Stack<Node>> undoStack = new Stack<Stack<Node>>();
            //if all nodes are reduced to a single possible value, finish
            if (DungeonGraph.areNodesInstantiated)
            {
                DungeonGraph.AllNodes.ForEach(room => Debug.WriteLine(room.PrintConnections()));
            }
            else
            {
                int frame = undoStack.Count;
                undoStack.Push(new Stack<Node>());
                List<Node> possible = DungeonGraph.AllNodes.FindAll(node => node.Values.Count > 1); //find all nodes whos values are not reduced to a singleton
                Node selected = possible[RNG.Next(0, possible.Count - 1)];
                foreach (List<Node> value in selected.Values)
                {
                    try
                    {
                        Reduce(selected, new List<List<Node>>() { value }, undoStack);
                        //if reduce didn't throw an exception, assign and repeat
                        DungeonGraph.Node(selected.Id).Values[0].ForEach(child => DungeonGraph.Connect(selected.Id, child.Id));
                        MapOne();

                    }
                    catch (Exception e)
                    {
                        while (undoStack.Count != frame)
                        {
                            Node savedNode = undoStack.Peek().Pop();
                            DungeonGraph.AllNodes[DungeonGraph.AllNodes.IndexOf(DungeonGraph.Node(savedNode.Id))] = savedNode;
                            if (undoStack.Peek().Count == 0) undoStack.Pop();
                        }
                    }

                    //undoStack.currentFrame = frame
                }
            }
        }

        /// <summary>
        /// Reduce the possible values to a single one
        /// </summary>
        /// <param name = "A" > Node to be reduced</param>
        /// <param name = "set" >Potential values for A</param>
        /// <returns>Valid reduction? A : null</returns>
        private void Reduce(Node A, List<List<Node>> set, Stack<Stack<Node>> undoStack)
        {
            if (set.Count == 0)
            {
                throw new Exception($"Reduced {A.Id} to empty set. Backtracking...");
            }
            if (//A.Values.Count == 1 &&
                !(A.Values.Except(set).ToList().Any() && set.Except(A.Values).ToList().Any())) // A.Values != set
            {
                undoStack.Peek().Push(new Node(A)); // save the pre-modified values
                A.Values = set;
                foreach (var other in A.Values)
                {
                    Propagate(other, A, undoStack); //other are the ones that are about to be connected
                }
            }
        }

        private void Propagate(List<Node> otherNodes, Node modA, Stack<Stack<Node>> undoStack)
        {
            foreach (Node child in otherNodes)
            {
                if (child.Id != modA.Id && !modA.IsConnected(child))
                {
                    List<List<Node>> allowedValues = new List<List<Node>>(); //ToDo: Compute possible values for v given all the node sets

                    // select the sets of values of the constrained var that ::

                    // include our modA (constraining, i.e. already set variable)
                    allowedValues = child.Values.FindAll(set => set.Contains(modA)); //the modA does not work because its been modified ==> will not be in
                    // do not invalidate the CPLength
                    foreach (var set in allowedValues.ToList())
                    {
                        foreach (var node in set)
                        {
                            if (node.Id != modA.Id && !DungeonGraph.ValidCPLength(node, modA))
                            {
                                allowedValues.Remove(set);
                            }

                        }
                    }

                    // do not exceed MaxNeighbours
                    foreach (var set in allowedValues.ToList())
                    {
                        foreach (var node in set)
                        {
                            if (!DungeonGraph.ValidNeighboursPostInc(node))
                            {
                                allowedValues.Remove(set);
                            }

                        }
                    }
                    Reduce(child, allowedValues, undoStack);
                }
            }
            Debug.WriteLine("Propagate finished");
        }

        /// <summary>
        /// Select a random Node that still has unconnected edges and attempt to connect them
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="rng"></param>
        /// <returns></returns>
        public SpaceGraph NeighbourMapper(SpaceGraph graph)
        {
            List<Node> unconnected = graph.AllNodes.FindAll(node => node.MaxNeighbours - node.Edges.Count > 0);
            int globalRetries = 100;
            while (unconnected.Count != 0 && globalRetries >= 0)
            {
                //Debug.WriteLine($"Unconnected nodes left {unconnected.Count}");
                Node parent = unconnected[RNG.Next(0, unconnected.Count)];
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

                    Node child = temp[RNG.Next(0, temp.Count)];

                    if (MapOne(parent, child) == null)
                    {
                        retries--;
                    }
                }
                if (unconnectedNeighbours < 0)
                {
                    Debug.WriteLine($"Finished [{parent.Id}], removing node...");
                    unconnected.Remove(parent);
                }
                else
                {
                    //       Debug.WriteLine($"Failed to match all neighbours for node {parent.Id}, retrting {globalRetries} times more");
                    globalRetries--;
                    ////add some noise in an attempt to avoid getting stuck. (no guarantees)
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
        public void DungeonSampler(params (FeatureType, int)[] observations)
        {
            DungeonModel.SetObservations(true, observations);
            _ = DungeonModel.Sample();
            //Get the global (Dungeon) parameters
            Rooms = (int)DungeonModel.Value(FeatureType.NumRooms);               //Hard constraint
            CPLength = (int)DungeonModel.Value(FeatureType.CriticalPathLength);  //Hard constraint
            Doors = (int)DungeonModel.Value(FeatureType.NumDoors);               //Soft constraint

            Debug.WriteLine($"Dungeon Sampled: [{Rooms},{CPLength},{Doors}]");
        }
        /// <summary>
        /// Sample room parameters from our SpaceModel and assign them to a node
        /// Must be run after evidence for the global dungeon parameters is set
        /// </summary>
        /// <param name="DungeonModel">Inference model</param>
        /// <param name="node">room</param>
        public Node RoomSampler(Node room)
        {
            //temp save of global dungeon params
            int tempRooms = Rooms;
            int tempCPLength = CPLength;
            int tempDoors = Doors;

            //1 set global observations and maintain them :: clear -> sample -> reset
            //2 set any known room observations (i.e. depth and cpDistance for Entrance node and cpDistance for rooms on the critical path) 
            //make sure you don't invalidate the hard constraints and log when the soft ones have to be adjusted

            DungeonModel.SetObservations(true,
                (FeatureType.NumRooms, tempRooms),
                (FeatureType.CriticalPathLength, tempCPLength),
                (FeatureType.NumDoors, tempDoors));


            if (room.CPDistance == 0 && room.Depth != null)  //room on critical path
            {
                DungeonModel.Observe(FeatureType.CriticalPathDistance, 0);
                DungeonModel.Observe(FeatureType.Depth, room.Depth.Value);

                _ = DungeonModel.Sample();

                room.MaxNeighbours = (int)DungeonModel.Value(FeatureType.NumNeighbours);    //Hard constraint
                room.Values = new List<List<Node>>(room.MaxNeighbours.Value);
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
                    room.Values = new List<List<Node>>(room.MaxNeighbours.Value);
                }
                else return RoomSampler(room);
            }


            return room;
        }
        #endregion



    }
}
