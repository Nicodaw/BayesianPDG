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

                double rooms = dungeonModel.Value(FeatureType.NumRooms);
                double cpl = dungeonModel.Value(FeatureType.CriticalPathLength);
                double doors = dungeonModel.Value(FeatureType.NumDoors);


                Debug.WriteLine($"Sampled: [{rooms},{cpl},{doors}]");

                SpaceGraph graph = new SpaceGraph();

                // ~ Building the Space graph for the map generator ~ //
                for (int i = 0; i < rooms; i++)
                {
                    graph.CreateNode(i);
                }

                for (int i = 0; i < cpl; i++)
                {
                    dungeonModel.ClearObservations();
                    dungeonModel.Observe(FeatureType.NumRooms, observedRooms);
                    _ = dungeonModel.Sample();
                    //ToDo: implement
                }
                foreach (Node node in graph.AllNodes)
                {
                    if (node.Id + 1 < graph.AllNodes.Count)
                    {
                        graph.Connect(node.Id, node.Id + 1); //connect this and the next one | dummy logic
                    }
                }

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
