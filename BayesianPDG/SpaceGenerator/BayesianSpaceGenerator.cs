using BayesianPDG.SpaceGenerator.Space;
using Netica;
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
                double rooms = dungeonModel.Value(FeatureType.NumRooms);          //Hard constraint
                double cpl = dungeonModel.Value(FeatureType.CriticalPathLength);  //Hard constraint
                double doors = dungeonModel.Value(FeatureType.NumDoors);          //Soft constraint

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

                foreach (Node node in graph.AllNodes)
                {
                    if (node.Id + 1 < graph.AllNodes.Count)
                    {
                        graph.Connect(node.Id, node.Id + 1); //connect this and the next one | dummy logic
                    }
                }

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

    }
}
