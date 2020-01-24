using BayesianPDG.SpaceGenerator.Space;
using Netica;
using System.Diagnostics;

namespace BayesianPDG.SpaceGenerator
{
    class BayesianSpaceGenerator
    {
        public static Application NeticaApp = new Application();
        public SpaceGraph RunInference()
        {
            DAGLoader dungeonBNLoader = new DAGLoader();
            SpaceModel dungeonModel = new SpaceModel(dungeonBNLoader);
            // DAGLoader DungeonEMLoader = new DAGLoader("Resources\\BNetworks\\DungeonNetEM.neta");
            //DAGLoader DungeonCountLoader = new DAGLoader("Resources\\BNetworks\\DungeonNetCount.neta");
            //DAGLoader DungeonGDLoader = new DAGLoader("Resources\\BNetworks\\DungeonNetGD.neta");
            try
            {
                //Configure observations, i.e. how many rooms in the dungeon
                int observedRooms = 6;
                dungeonModel.Observe(FeatureType.NumRooms, observedRooms);
                BNet sample = dungeonModel.Sample();

                double rooms = dungeonModel.Value(FeatureType.NumRooms);
                double cpl = dungeonModel.Value(FeatureType.CriticalPathLength);
                double doors = dungeonModel.Value(FeatureType.NumDoors);

                Debug.WriteLine($"Sampled: [{rooms},{cpl},{doors}]");

                SpaceGraph graph = new SpaceGraph();

                //Start building the Space graph for the map generator
                for (int i = 0; i < rooms; i++)
                {
                    graph.CreateNode(i);
                }

                for (int i = 0; i < cpl; i++)
                {
                    //ToDo: implement
                }
                foreach (Node node in graph.AllNodes)
                {
                    if (node.Id + 1 < graph.AllNodes.Count)
                    {
                        graph.Connect(node.Id, node.Id + 1); //connect this and the next one
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
