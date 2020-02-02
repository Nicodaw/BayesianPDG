using Netica;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BayesianPDG.SpaceGenerator
{
    class SpaceModel
    {
        private const int UndefinedState = -3; //netica default undefined state value
        private readonly Application _app = BayesianSpaceGenerator.NeticaApp;
        private BNet net { get; }
        private Caseset data { get; } // ToDo: remove? Do we need the data files if we're loading a pre-trained net?

        public SpaceModel(DAGLoader loader)
        {

            net = loader.Net;
            data = loader.Data;

        }

        /// <summary>
        /// Sample the network. Randomly observes values depending on their distribution. Sample() takes in account previously set observations and performs inference with those facts.
        /// I.e. if you have Observed NumRooms = 5 it will run the sampler on the updated CPT where it knows that there will be 5 rooms.
        /// Because Netica BNets are stateful, if you want to run the sampler multiple times, you have to clear the findings by running ClearObservations().
        /// That will clear observations you have set manually.
        /// 
        /// A simple retry mechanism is implemented. The assumption is that when there is a failure, we fix the global Dungeon parameters and resample the room
        /// by propagating the dungeon parameters.
        /// </summary>
        /// <param name="retry">How many times to retry before failing. Set retry = 0 if you want imediate failure on invalid sample.</param>
        public BNet Sample(int retry = 10)
        {
            int exit = net.GenerateRandomCase(net.Nodes);
            bool isValid = ValidateSample();// ToDo: Remove temporary hardcoded validation upon finishing the dataset. Add retry.

            if (exit == 0 && isValid) return net;
            else
            {
                if(retry != 0)
                {
                    Debug.WriteLine($"There was a problem Sampling the network. Exit code {exit}. Is sample valid? {isValid}. Retrying...");
                    var priorObservations = (rooms: (int) Value(FeatureType.NumRooms), doors: (int) Value(FeatureType.NumDoors), cpLength: (int) Value(FeatureType.CriticalPathLength));
                    ClearObservations();
                    Observe(FeatureType.NumRooms, priorObservations.rooms);
                    Observe(FeatureType.NumDoors, priorObservations.doors);
                    Observe(FeatureType.CriticalPathLength, priorObservations.cpLength);
                    return Sample(retry--);
                }
                else
                {
                    ClearObservations();
                    throw new InvalidOperationException($"There was a problem Sampling the network and it failed after retring 10 times. Exit code {exit}. Is sample valid? {isValid}.");
                }
                
            }

        }

        #region Public Methods
        /// <summary>
        /// Set evidence for a particular node. This is a wrapper for EnterFindings so we can use values instead of indices.
        /// </summary>
        /// <param name="node">target RV</param>
        /// <param name="state">The value of the evindence.</param>
        public void Observe(BNode node, int state)
        {
            try
            {
                int index = UndefinedState;
                for (int i = 0; i < node.NumberStates; i++)
                {
                    if (node.StateLabel[i].Equals(state.ToString())) index = i; //iterating over StateLabels. Names are not set and hence we can't use node.GetStateIndex(str statename)
                }
                node.EnterFinding(index);
            }
            catch (COMException e)
            {
                throw new COMException($"No state {state} could be found for node {node.Name}", e);
            }
        }
        public void Observe(string node, int state) => Observe(net.Nodes.get_Item(node), state);
        public void Observe(FeatureType node, int state) => Observe(node.ToString(), state);
        /// <summary>
        /// Extract belief for a particular node. This is a wrapper for GetBelief so we can use values instead of indices.
        /// </summary>
        /// <param name="node">target RV</param>
        /// <param name="state">The value of the evindence.</param>
        public double Probability(BNode node, int state)
        {
            try
            {
                int index = UndefinedState;
                for (int i = 0; i < node.NumberStates; i++)
                {
                    if (node.StateLabel[i].Equals(state.ToString())) index = i; //iterating over StateLabels. Names are not set and hence we can't use node.GetStateIndex(str statename)
                }
                return node.GetBelief(index);
            }
            catch (COMException e)
            {
                throw new COMException($"No state {state} could be found for node {node.Name}", e);
            }
        }
        public double Probability(string node, int state) => Probability(net.Nodes.get_Item(node), state);
        public double Probability(FeatureType node, int state) => Probability(node.ToString(), state);

        /// <summary>
        /// Get a point estimate from a sampled BN
        /// Note: The value must be observed otherwise it will throw an exception
        /// </summary>
        /// <param name="node">target RV</param>
        /// <returns></returns>
        public double Value(BNode node)
        {
            double val = node.CalcValue();
            if (node.CalcState() != UndefinedState)
            {
                return val;
            }
            else
            {
                throw new COMException($"No value is set for node {node.Name}. Make sure you have either set the observation of the node or have ran Sample()");
            }
        }
        public double Value(string node) => Value(net.Nodes.get_Item(node));
        public double Value(FeatureType node) => Value(node.ToString());

        public void ClearObservations() => net.RetractFindings();
        public void ClearObservations(FeatureType node) => net.Nodes.get_Item(node.ToString()).RetractFindings();
        #endregion


        #region Utils
        private void ClearCPT()
        {
            foreach (BNode node in net.Nodes)
            {
                node.DeleteTables();
            }
        }

        /// <summary>
        /// Checks for our heuristic clauses that must hold for the sample to be valid
        /// </summary>
        /// <returns>is the sample valid</returns>
        private bool ValidateSample()
        {
            bool isCPTValid = Value(FeatureType.NumRooms) >= Value(FeatureType.CriticalPathLength); //critical path cannot be longer than the number of rooms
            bool areDoorsValid = Value(FeatureType.NumDoors) >= Value(FeatureType.NumNeighbours);   //a room cannot have more neighbours than there are total number of connections in the map
            bool isDepthValid = Value(FeatureType.NumRooms) >= Value(FeatureType.Depth);            //a room cannot be at depth greater than the total number of rooms  

            var isValid = (isCPTValid, areDoorsValid, isDepthValid) switch
            {
                (false, true, true) => (false, "CPT is invalid"),
                (true, false, true) => (false, "Doors are invalid"),
                (true, true, false) => (false, "Depth is invalid"),
                _ => (true, "Everything is valid")
            };
            Debug.WriteLine($"Room validator reports:: {isValid.Item2}");
            return isValid.Item1;
        }
        #endregion
    }
}
