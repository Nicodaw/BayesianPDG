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
        private readonly Application _app = BayesianSpaceGenerator.NeticaApp;
        private BNet net { get; }
        private Caseset data { get; }

        public SpaceModel(DAGLoader loader)
        {

            net = loader.Net;
            data = loader.Data;

            //double rooms = Probability(NodeTypes.NumRooms, 3);
            //Debug.WriteLine("The probability of a dungeon having 6 Rooms is " + rooms.ToString("G4"));
            //double cpr = Probability(NodeTypes.CriticalPathLength, 3);
            //Debug.WriteLine("The probability of a critical path being 3 is " + cpr.ToString("G4"));
            //Observe(NodeTypes.NumRooms, 2);
            //cpr = Probability(NodeTypes.CriticalPathLength, 3);
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

        /// <summary>
        /// Learn using EM TODO: more descriptive summary of how EM is used
        /// </summary>
        /// <param name="withClearTables">If the nodes have CPT and experience tables before the learning starts, they will be considered as part of the data .
        /// Set if you don't want these observations to be taken in account. Note: It clears all previously set CPTs</param>
        /// <returns>EM Learner</returns>
        Learner LearnEM(bool withClearTables = true)
        {
            if (withClearTables) ClearCPT();
            Learner em = _app.NewLearner(LearningMethod.EMLearning);
            em.LearnCPTs(net.Nodes, data, 1);
            return em;
        }

        #region Public Methods
        /// <summary>
        /// Set evidence for a particular node. This is a wrapper for EnterFindings so we can use values instead of indices.
        /// </summary>
        /// <param name="node">target node</param>
        /// <param name="state">The value of the evindence.</param>
        public void Observe(BNode node, int state)
        {
            try
            {
                int index = -3; //netica default undefined state value
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
        public void Observe(NodeTypes node, int state) => Observe(node.ToString(), state);
        /// <summary>
        /// Extract belief for a particular node. This is a wrapper for GetBelief so we can use values instead of indices.
        /// </summary>
        /// <param name="node">target node</param>
        /// <param name="state">The value of the evindence.</param>
        public double Probability(BNode node, int state)
        {
            try
            {
                int index = -3; //netica default undefined state value
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
        public double Probability(NodeTypes node, int state) => Probability(node.ToString(), state);
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
