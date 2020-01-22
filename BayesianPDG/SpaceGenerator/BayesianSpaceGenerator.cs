using Netica;
using System.Diagnostics;

namespace BayesianPDG.SpaceGenerator
{
    class BayesianSpaceGenerator
    {
        public static Application NeticaApp = new Application();
        public void RunInference() 
        {
            DAGLoader dungeonBNLoader = new DAGLoader();
            SpaceModel dungeonModel = new SpaceModel(dungeonBNLoader);
            //DAGLoader DungeonEMLoader = new DAGLoader("Resources\\BNetworks\\DungeonNetEM.neta");
            //DAGLoader DungeonCountLoader = new DAGLoader("Resources\\BNetworks\\DungeonNetCount.neta");
            //DAGLoader DungeonGDLoader = new DAGLoader("Resources\\BNetworks\\DungeonNetGD.neta");

            dungeonModel.Observe(NodeTypes.NumRooms, 6);
            //BNet sample = dungeonModel.Sample();
            BNet sample = dungeonBNLoader.Net;

            //Get the Samples
            foreach (BNode node in sample.Nodes)
            {
                Debug.WriteLine($"{node.Name} state[{node.CalcState()}] = {node.CalcValue()}");
            }


            dungeonBNLoader.close();
        }
    }
}
