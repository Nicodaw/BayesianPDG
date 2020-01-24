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
        private Caseset data { get; }

        public SpaceModel(DAGLoader loader)
        {

            net = loader.Net;
            data = loader.Data;

            //double rooms = Probability(FeatureType.NumRooms, 3);
            //Debug.WriteLine("The probability of a dungeon having 6 Rooms is " + rooms.ToString("G4"));
            //double cpr = Probability(FeatureType.CriticalPathLength, 3);
            //Debug.WriteLine("The probability of a critical path being 3 is " + cpr.ToString("G4"));
            //Observe(FeatureType.NumRooms, 2);
            //cpr = Probability(FeatureType.CriticalPathLength, 3);
            //Debug.WriteLine("Given 6 rooms, the probability of a critical path being 3 is " + cpr.ToString("G4"));
            //net.RetractFindings();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clearFindings">Remove previously set observations.</param>
        public BNet Sample(bool clearFindings = false)
        {
            if (clearFindings)
            {
                net.RetractFindings();
            }
            int exit = net.GenerateRandomCase(net.Nodes);

            if (exit == 0)
            {
                return net;
            }
            else
            {
                net.RetractFindings();
                throw new InvalidOperationException("There was a problem Sampling the network");
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
            } else
            {
                throw new COMException($"No value is set for node {node.Name}. Make sure you have either set the observation of the node or have ran Sample()");
            }
        }
        public double Value(string node) => Value(net.Nodes.get_Item(node));
        public double Value(FeatureType node) => Value(node.ToString());
        #endregion


        #region Utils
        private void ClearCPT()
        {
            foreach (BNode node in net.Nodes)
            {
                node.DeleteTables();
            }
        }
        #endregion
    }
}
