namespace BayesianPDG.SpaceGenerator
{
    class BayesianSpaceGenerator
    {
        public void RunInference() 
        {
            DAGLoader BNLoader = new DAGLoader();
            _ = new SpaceModel(BNLoader.Net);
            BNLoader.close();
        }
    }
}
