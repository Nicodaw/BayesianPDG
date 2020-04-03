using BayesianPDG.SpaceGenerator.Space;
using Netica;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BayesianPDG.SpaceGenerator
{
    public class BayesianSpaceGenerator
    {
        /// <summary>
        /// Global reference to the Netica COM
        /// </summary>
        public Application NeticaApp = new Application();

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
        public SpaceGraph DungeonGraph;

        public static (int cap, int cur) attempts = (5000, 0); //we allow for 5000 attempts at making a dungeon
        public (int cap, int cur) retries = (10000, 10000);       //we allow for 10K backpropagations

        #region Dungeon Parameters
        private int Rooms;
        private int CPLength;
        #endregion

        public SpaceGraph RunInference(int observedRooms, string path, int? seed = null)
        {
            attempts.cur++;
            DAGLoader dungeonBNLoader = new DAGLoader(path, NeticaApp);
            DungeonModel = new SpaceModel(dungeonBNLoader);
            RNG = (seed == null) ? new Random() : new Random(seed.Value);
            //Configure observations, i.e. how many rooms in the dungeon
            DungeonGraph = new SpaceGraph();

            DungeonSampler((FeatureType.NumRooms, observedRooms)); // For this experiment we are observing only the total number of rooms according to user specification.


            Console.WriteLine($"Dungeon Sampled: [{Rooms},{CPLength}]");
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

            Console.WriteLine("=== Final set of rooms sampled ===");
            Console.WriteLine("=== [cpDistance, depth, maxNe] ===");
            DungeonGraph.AllNodes.ForEach(node => Console.WriteLine($"[{node.CPDistance},{node.Depth},{node.MaxNeighbours}]"));

            // Initialise the potential connections for formulating the CSP
            // We're externally enforcing the cardinality constraint by instantiating the Values as a K combinatorial set of the N rooms ( K = MaxNeighbours for each node.Values)
            DungeonGraph.ReducePotentialValues();


            Console.WriteLine("Potential room connections after CP mapping");
            DungeonGraph.AllNodes.ForEach(node => Console.WriteLine(node.PrintConnections()));

            // Transform the basis dungeon in a per-room fashion
            // Compose a list of all nodes that still don't have their full neighbours reached
            // randomly assign neighbours and remove any fully connected node from the list
            // repeat untill all have been assigned
            // the data handles some constraints implicitly (e.g. data guarantees that there will be no room with MaxNeighbours == 0)

            try
            {
                Map();

                // Validate if graph is complete and print/write all metrics before returning the complete graph
                Console.WriteLine($"Is DungeonGraph complete? {DungeonGraph.isComplete}");
                Console.WriteLine($"Is DungeonGraph planar? {DungeonGraph.isPlanar}");
                List<Node> unconnected = DungeonGraph.AllNodes.FindAll(node => node.MaxNeighbours - node.Edges.Count > 0);
                List<Node> connected = DungeonGraph.AllNodes.FindAll(node => node.MaxNeighbours - node.Edges.Count == 0);
                string unc = string.Join(", ", unconnected.Select(x => x.Id).ToArray());
                string con = string.Join(", ", connected.Select(x => x.Id).ToArray());
                Console.WriteLine($"List of Unconected nodes {unc}");
                Console.WriteLine($"List of Connected nodes {con}");
                Console.Write(DungeonGraph.ToString());
                dungeonBNLoader.close();
                string metricsPath = $"Resources\\BNetworks\\attempts_log_{observedRooms}.csv";
                if (!File.Exists(metricsPath))
                {
                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(metricsPath))
                    {
                        sw.WriteLine("Attempts");
                    }
                }
                using (StreamWriter sw = File.AppendText(metricsPath))
                {
                    sw.WriteLine(attempts.cur);
                }


                return DungeonGraph;
            }
            catch (ArgumentException ioe)
            {
                if (attempts.cur < attempts.cap)
                {
                    Console.WriteLine($"{ioe.Message}. Retrying...");
                    return RunInference(observedRooms, path);
                }
                else return null;
            }
        }

        public SpaceGraph RunRandomGraphInitialiser(int observedRooms, int? seed = null)
        {
            RNG = (seed == null) ? new Random() : new Random(seed.Value);
            DungeonGraph = new SpaceGraph();

            for (int i = 0; i < observedRooms; i++)
            {
                DungeonGraph.CreateNode(i);
            }

            RandomMapper();

            return DungeonGraph;
        }
        #region Graph transforms
        /// <summary>
        /// Randomly select nodes and connect them until no dangling nodes remain
        /// Dummy logic, does not maintain invariants
        /// </summary>
        private SpaceGraph RandomMapper()
        {
            int rooms = DungeonGraph.AllNodes.Count;

            while (!DungeonGraph.isComplete)
            {
                List<Node> possible = DungeonGraph.AllNodes.FindAll(node => node.Edges.Count == 0); //find all nodes whList<Node> possible = DungeonGraph.AllNodes.FindAll(node => node.MaxNeighbours < node.Edges.Count);
                Node randParent = (possible.Count <= 1) ? null : possible[RNG.Next(0, possible.Count - 1)];
                Node randChild;
                if (randParent != null)
                {
                    randChild = possible.Where(node => node.Id != randParent.Id).ToList()[RNG.Next(0, possible.Count - 1)];
                }
                else
                {
                    List<Node> disconnected = DungeonGraph.AllNodes.FindAll(node => node.Id != 0 && DungeonGraph.PathTo(0, node.Id).Count == 0);
                    List<Node> connected = DungeonGraph.AllNodes.FindAll(node => node.Id == 0 || DungeonGraph.PathTo(0, node.Id).Count != 0);
                    randParent = (disconnected.Count == 1) ? disconnected[0] : disconnected[RNG.Next(0, disconnected.Count - 1)];
                    randChild = (connected.Count == 1) ? connected[0] : connected[RNG.Next(0, connected.Count - 1)];
                }
                DungeonGraph.Connect(randParent, randChild);
            }
            return DungeonGraph;
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

        public void Map()
        {
            while (!DungeonGraph.areNodesInstantiated && retries.cur > 0)
            {
                MapOne();
            }
            try
            {
                DungeonGraph.InstantiateGraph();
            }
            catch (ArgumentException ioe)
            {
                throw ioe;
            }
        }

        private void MapOne()
        {

            Stack<Stack<Node>> undoStack = new Stack<Stack<Node>>();
            //if all nodes are reduced to a single possible value, finish
            if (DungeonGraph.areNodesInstantiated)
            {
                Debug.WriteLine("=== Solution === ");
                DungeonGraph.AllNodes.ForEach(room => Debug.WriteLine(room.PrintConnections()));
            }
            else
            {
                List<Node> possible = DungeonGraph.AllNodes.FindAll(node => node.Values.Count > 1); //find all nodes whList<Node> possible = DungeonGraph.AllNodes.FindAll(node => node.MaxNeighbours < node.Edges.Count);
                Node selected = (possible.Count == 1) ? possible[0] : possible[RNG.Next(0, possible.Count - 1)];
                List<Node> value = selected.Values[0];
                int frame = undoStack.Count;
                undoStack.Push(new Stack<Node>());
                try
                {
                    Reduce(selected, new List<List<Node>>() { value }, undoStack);
                    //if reduce didn't throw an exception, assign and repeat
                    DungeonGraph.Node(selected.Id).Values[0].ForEach(child => DungeonGraph.Connect(selected.Id, child.Id));
                    retries.cur = retries.cap;
                    MapOne();

                }
                catch (Exception)
                {
                    while (undoStack.Count != frame)
                    {
                        Node savedNode = undoStack.Peek().Pop();
                        savedNode.Values.Remove(value); //remove what didn't work
                        DungeonGraph.AllNodes[DungeonGraph.AllNodes.IndexOf(DungeonGraph.Node(savedNode.Id))] = savedNode;
                        if (undoStack.Peek().Count == 0) undoStack.Pop();
                    }
                }
                finally
                {
                    retries.cur--;
                }
            }
        }

        /// <summary>
        /// Reduce the possible values to a single one
        /// </summary>
        /// <param name = "A" > Node to be reduced</param>
        /// <param name = "set" >Potential values for A</param>
        private void Reduce(Node A, List<List<Node>> set, Stack<Stack<Node>> undoStack)
        {
            if (set.Count == 0)
            {
                throw new Exception($"Reduced {A.Id} to empty set. Backtracking...");
            }
            if (!A.ValuesEqual(set))
            {
                undoStack.Peek().Push(new Node(A)); // save the pre-modified values
                A.Values = set;
                foreach (var other in A.Values)
                {
                    Propagate(other, A, undoStack); //other are the ones that are about to be connected
                }
            }
        }

        private void Propagate(List<Node> neighbours, Node modA, Stack<Stack<Node>> undoStack)
        {
            foreach (Node child in neighbours)
            {
                List<List<Node>> allowedValues = new List<List<Node>>();

                // select the sets of values of the constrained var that ::

                // in the child node that is to be connected, find only the possible values that include the parent (modA)
                allowedValues = child.Values.FindAll(set => set.Contains(modA)); //Comparison is done only based on id

                // do not invalidate the CPLength
                foreach (var set in allowedValues.ToList())
                {
                    foreach (var node in set)
                    {
                        if (!node.IsConnected(child) && !DungeonGraph.ValidCPLength(node, child))
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

                        if (!node.IsConnected(child) && (!DungeonGraph.ValidNeighboursPostInc(node) || !DungeonGraph.ValidNeighboursPostInc(child)))
                        {
                            allowedValues.Remove(set);
                        }

                    }
                }

                // do not invalidate Depth
                foreach (var set in allowedValues.ToList())
                {
                    foreach (var node in set)
                    {

                        if (!node.IsConnected(child) && !DungeonGraph.ValidDepth(node, child))
                        {
                            allowedValues.Remove(set);
                        }

                    }
                }

                // do not invalidate CP distance
                foreach (var set in allowedValues.ToList())
                {
                    foreach (var node in set)
                    {
                        if (!node.IsConnected(child) && !DungeonGraph.ValidCPDistance(node, child))
                        {
                            allowedValues.Remove(set);
                        }

                    }
                }

                // do not invalidate planarity
                foreach (var set in allowedValues.ToList())
                {
                    foreach (var node in set)
                    {
                        if (!node.IsConnected(child) && !DungeonGraph.ValidPlanarGraph(node, child))
                        {
                            allowedValues.Remove(set);
                        }
                    }
                }

                Reduce(child, allowedValues, undoStack);
            }
            Debug.WriteLine("Propagate finished");
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

            //1 set global observations and maintain them :: clear -> sample -> reset
            //2 set any known room observations (i.e. depth and cpDistance for Entrance node and cpDistance for rooms on the critical path) 
            //make sure you don't invalidate the hard constraints and log when the soft ones have to be adjusted

            DungeonModel.SetObservations(true,
                (FeatureType.NumRooms, tempRooms),
                (FeatureType.CriticalPathLength, tempCPLength));


            if (room.CPDistance == 0 && room.Depth != null)  //room on critical path
            {
                DungeonModel.Observe(FeatureType.CriticalPathDistance, 0);
                DungeonModel.Observe(FeatureType.Depth, room.Depth.Value);

                _ = DungeonModel.Sample();
                int maxNeigh = (int)DungeonModel.Value(FeatureType.NumNeighbours); //Hard constraint

                if (room.Id != DungeonGraph.Entrance.Id && room.Id != DungeonGraph.Goal.Id && maxNeigh < 2)
                {
                    return RoomSampler(room);
                }
                else if (maxNeigh <= tempRooms)
                {
                    room.MaxNeighbours = maxNeigh;
                    room.Values = new List<List<Node>>(room.MaxNeighbours.Value);
                }
                else return RoomSampler(room);
            }
            else // room not on critical path
            {
                _ = DungeonModel.Sample();

                int depth = (int)DungeonModel.Value(FeatureType.Depth);                     //Soft constraint
                int maxNeigh = (int)DungeonModel.Value(FeatureType.NumNeighbours);          //Hard constraint
                int cpDistance = (int)DungeonModel.Value(FeatureType.CriticalPathDistance); //Hard constraint

                if (depth != 0 && cpDistance != 0 && maxNeigh <= tempRooms)
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
