using Netica;

namespace BayesianPDG.SpaceGenerator
{
    class BayesianSpaceGenerator
    {
        public static Application NeticaApp = new Application();
        public void RunInference() 
        {
            DAGLoader BNLoader = new DAGLoader();
            _ = new SpaceModel(BNLoader);
            BNLoader.close();
        }
    }
}
