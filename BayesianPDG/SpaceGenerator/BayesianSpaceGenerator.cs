﻿using BayesianPDG.SpaceGenerator.Space;
using Netica;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
        public SpaceGraph DungeonGraph;

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

                int observedRooms = 9; // ToDo: Let user decide

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

                Map();

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

        public void Map()
        {
           // foreach (Node node in DungeonGraph.AllNodes.ToList())
            while(!DungeonGraph.areNodesInstantiated)
            {
                MapOne();
            }

            DungeonGraph.InstantiateGraph();
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
                List<Node> possible = DungeonGraph.AllNodes.FindAll(node => node.Values.Count > 1); //find all nodes whos values are not reduced to a singleton
                Node selected = (possible.Count == 1) ? possible[0] : possible[RNG.Next(0, possible.Count - 1)];
                //foreach (List<Node> value in selEnum.Values)
               // for (int i = 0; i < selected.Values.ToList().Count; i++)
               while(selected.Values.Count != 1)
                {
                    List<Node> value = selected.Values[0];
                    int frame = undoStack.Count;
                    undoStack.Push(new Stack<Node>());
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
                            savedNode.Values.Remove(value); //remove what didn't work
                            selected = savedNode;
                            DungeonGraph.AllNodes[DungeonGraph.AllNodes.IndexOf(DungeonGraph.Node(savedNode.Id))] = savedNode;
                            if (undoStack.Peek().Count == 0) undoStack.Pop();
                        }
                    }
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
                //if (child.Id != modA.Id && !modA.IsConnected(child) && child.Values.Count != 1)
                //{
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
                    Reduce(child, allowedValues, undoStack);
                }
            //}
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
