using Netica;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BayesianPDG.SpaceGenerator
{
    class SpaceModel
    {
        private  BNet Net { get; }

        public void Observe(BNode node, object state) => node.EnterFinding(state);
        public void Observe(string node, object state) => Observe(Net.Nodes.get_Item(node), state);
        public void Observe(NodeTypes node, object state) => Observe(node.ToString(), state);
        public double Probability(BNode node, object state) => node.GetBelief(state);
        public double Probability(string node, object state) => Probability(Net.Nodes.get_Item(node), state);
        public double Probability (NodeTypes node, object state)=> Probability(node.ToString(), state);

        public SpaceModel(BNet net)
        {
            Net = net;

            double rooms = Probability(NodeTypes.NumRooms,5);

            Debug.WriteLine("The probability of a dungeon having 5 Rooms is " + rooms.ToString("G4"));

            double cpr = Probability(NodeTypes.CriticalPathLength, 3);

            Debug.WriteLine("The probability of a critical path being 3 is " + cpr.ToString("G4"));

            Observe(NodeTypes.NumRooms, 7);

            cpr = Probability(NodeTypes.CriticalPathLength, 3);

            Debug.WriteLine("Given 7 rooms, the probability of a critical path being 3 is " + cpr.ToString("G4"));
        }


    }
}
