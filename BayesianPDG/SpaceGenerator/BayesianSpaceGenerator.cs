using BayesianPDG.SpaceGenerator.Space;
using Netica;
using System;
using System.Diagnostics;

namespace BayesianPDG.SpaceGenerator
{
    class BayesianSpaceGenerator
    {
        /// <summary>
        /// Global reference to the Netica COM
        /// </summary>
        public static Application NeticaApp = new Application();
        public SpaceGraph RunInference()
        {
            DAGLoader dungeonBNLoader = new DAGLoader("Resources\\BNetworks\\EMNet.neta");
            SpaceModel dungeonModel = new SpaceModel(dungeonBNLoader);
            try
            {
                //Configure observations, i.e. how many rooms in the dungeon
                int observedRooms = 6; // ToDo: Let user decide
                dungeonModel.Observe(FeatureType.NumRooms, observedRooms);

                _ = dungeonModel.Sample();
                //Get the global (Dungeon) parameters
                int rooms = (int)dungeonModel.Value(FeatureType.NumRooms);          //Hard constraint
                int cpl = (int)dungeonModel.Value(FeatureType.CriticalPathLength);  //Hard constraint
                int doors = (int)dungeonModel.Value(FeatureType.NumDoors);          //Soft constraint

                //ToDo: support for the local (Room) parameters

                Debug.WriteLine($"Sampled: [{rooms},{cpl},{doors}]");

                SpaceGraph graph = new SpaceGraph();

                // ~ Building the Space graph for the map generator ~ //
                for (int i = 0; i < rooms; i++)
                {
                    graph.CreateNode(i);
                }

                // Create the critical path and set the distance for all nodes on it to 0
                for (int parent = 0; parent < cpl ; parent++)
                {
                    int child = (parent != cpl - 1) ? parent + 1 : graph.Goal.Id;
                    graph.Node(parent).CPDistance = 0;
                    graph.Node(child).CPDistance = 0;
                    graph.Connect(parent, child);
                }

                // Transform the basis dungeon in a per-room fashion
                for (int i = 0; i < rooms; i++)
                {
                    //set Observations and maintain them :: clear -> sample -> reset
                    //make sure you don't invalidate the hard constraints and log when the soft ones have to be adj

                    //dungeonModel.ClearObservations();
                    //dungeonModel.Observe(FeatureType.NumRooms, observedRooms);
                    //_ = dungeonModel.Sample();
                    //ToDo: room allocation
                }

                RandomConnect(graph);

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

    }
}
