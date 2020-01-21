using MissionGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BayesianPDG.MissionGenerator
{
    class Mission
    {
        public List<Vertex> Vertices { get; set; }
        public List<Edge> Edges { get; set; }

        public Mission() 
        {
            Vertices = new List<Vertex> ();
            Edges = new List<Edge>();

        }

        #region Mission Generation
        public void StartGraph()
        {
            Vertex startV =  createVertex(RoomType.Start,true);
            Vertex goalV = createVertex(RoomType.Goal,true);
            connect(startV, goalV, false);
            applyr1(startV, goalV);
        }

        public void GenerateDungeon(int size, int optional)
        {
            StartGraph();
            Random rnd = new Random();
            int rulechoice;
            int failchoice = 0;
            bool success = false;
            Vertex StartV = Vertices.ElementAt(0);
            Vertex GoalV = Vertices.ElementAt(1);
            applyr1(StartV, GoalV);
            for (int i = 0; i < size; i++)
            {
                rulechoice = rnd.Next(4) + 2;
                while (rulechoice == failchoice)
                { //making sure it cannot infinitely fail to match
                    rulechoice = rnd.Next(4) + 2;
                }
                switch (rulechoice)
                {
                    case 2:
                        success = matchr2();
                        break;
                    case 3:
                        success = matchr3();
                        break;
                    case 4:
                        success = matchr4();
                        break;
                    case 5:
                        success = matchr5();
                        break;
                }
                if (!success)
                { //if it didn't find a match for the chosen rule try again
                    i -= 1;
                    failchoice = rulechoice;
                }
            }
            //creating the optional rewards
            if (optional > 0)
            {
                for (int i = 0; i < optional; i++)
                {
                    matchr6();
                }
                int numOpLock = rnd.Next(optional);
                for (int i = 0; i < numOpLock; i++)
                {
                    matchr7();
                }
            }
        }


        #endregion

        //returns the edge connecting if v1 has a node connecting to v2 that isn't a RoomType.Key edge
        public Edge connected(Vertex v1, Vertex v2)
        {
            List<Edge> v1out = v1.Outgoing;
            List<Edge> v2in = v2.Incoming;

            foreach (Edge e in v1out)
            {
                if (v2in.Contains(e) && !e.HasKey)
                {
                    return e;
                }
            }
            return null;
        }

        //creates an edge connecting two nodes, can be a RoomType.Key
        public void connect(Vertex v1, Vertex v2, bool hasKey)
        {
            Edge e1 = createEdge(hasKey);
            v1.AddOutgoing(e1);
            v2.AddIncoming(e1);
        }

        private Vertex createVertex(RoomType type, bool isCritical)
        {
            Vertex v = new Vertex(Vertices.Count, isCritical, type);
            Vertices.Add(v);
            return v;
        }
        private Edge createEdge(bool hasKey)
        {
            Edge e = new Edge(Edges.Count, hasKey);
            Edges.Add(e);
            return e;
        }

        private void deleteEdge(int id)
        {
            Edge edge1 = Edges.Find(e => e.Id == id);
            edge1.Source.RemoveOutgoing(edge1);
            edge1.Target.RemoveIncoming(edge1);
            Edges.Remove(edge1);
        }

        //There are two parts to each rule, a checker that takes the vertexs and returns whether or not they're applicable
        //The checker now also holds an invariant that if any rule would increase the number of vertexs to more than 4, it fails
        private bool checkr1(Vertex startV, Vertex goalV)
        {
            if (connected(startV, goalV) != null)
            {
                if ((startV.Type == RoomType.Start) && (goalV.Type == RoomType.Goal))
                {
                    return startV.IsCritical && goalV.IsCritical;
                }
            }
            return false;
        }
        private void applyr1(Vertex startV, Vertex goalV)
        {
            if (checkr1(startV, goalV))
            {
                deleteEdge(connected(startV, goalV).Id);
                Vertex v1 = createVertex(RoomType.Lock, true);
                Vertex v2 = createVertex(RoomType.Key, true);
                connect(startV, v1, false);
                connect(startV, v2, false);
                connect(v2, v1, true); //creating the RoomType.Key connection
                connect(v1, goalV, false); //creating the connection between RoomType.Lock and goal
            }
        }
        private bool checkr2(Vertex lockV, Vertex goalV)
        {
            if (connected(lockV, goalV) != null)
            {
                if ((lockV.Type == RoomType.Lock) && (goalV.Type == RoomType.Goal)) {
                    if (lockV.IsCritical && goalV.IsCritical)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private void applyr2(Vertex lockV, Vertex goalV)
        {
            if (checkr2(lockV, goalV))
            {
                deleteEdge(connected(lockV, goalV).Id);
                Vertex v1 = createVertex(RoomType.Lock, true);
                Vertex v2 = createVertex(RoomType.Key, true);
                connect(lockV, v1, false);
                connect(lockV, v2, false);
                connect(v2, v1, true); //creating the RoomType.Key connection
                connect(v1, goalV, false); //creating the connection between RoomType.Lock and goal
            }
        }
        private bool checkr3(Vertex blankV, Vertex lockV, Vertex goalV)
        {
            if (connected(lockV, goalV) != null)
            {
                if (connected(blankV, lockV) != null)
                {
                    if (connected(blankV, goalV) != null)
                    {
                        return false;
                    }
                    else
                    {
                        if (blankV.IsCritical && lockV.IsCritical && goalV.IsCritical)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private void applyr3(Vertex blankV, Vertex lockV, Vertex goalV)
        {
            if (checkr3(blankV, lockV, goalV))
            {
                deleteEdge(connected(lockV, goalV).Id);
                Vertex v1 = createVertex(RoomType.Lock, true);
                Vertex v2 = createVertex(RoomType.Key, true);
                connect(lockV, v1, false); //connecting from the start RoomType.Lock to the new RoomType.Lock 
                connect(blankV, v2, false); //connecting from the blank node to the RoomType.Key
                connect(v2, v1, true); //creating the RoomType.Key connection
                connect(v1, goalV, false); //connecting between RoomType.Lock and goal
            }
        }
        private bool checkr4(Vertex blankS, Vertex blankP, Vertex blankG)
        {
            if ((connected(blankS, blankP) != null) && (connected(blankP, blankG) != null))
            {
                if (connected(blankS, blankG) == null)
                {
                    if (blankS.IsCritical && blankG.IsCritical)
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        private void applyr4(Vertex blankS, Vertex blankP, Vertex blankG)
        {
            if (checkr4(blankS, blankP, blankG))
            {
                Vertex v1 = createVertex(RoomType.Lock, true);
                Vertex v2 = createVertex(RoomType.Key, true);
                connect(blankS, v1, false);
                connect(blankS, v2, false);
                connect(v2, v1, true);
                connect(v1, blankG, false);
            }
        }
        private bool checkr5(Vertex blank1, Vertex blank2)
        {
            if (connected(blank1, blank2) != null)
            {
                if (blank1.IsCritical && blank2.IsCritical)
                {
                    return true;
                }
            }
            return false;
        }
        private void applyr5(Vertex blank1, Vertex blank2)
        {
            if (checkr5(blank1, blank2))
            {
                deleteEdge(connected(blank1, blank2).Id);
                Vertex v1 = createVertex(RoomType.Encounter, true);
                connect(blank1, v1, false);
                connect(v1, blank1, false);
            }
        }
        private bool checkr6(Vertex blank1)
        {
            if (blank1.IsCritical)
            {
                return true;
            }
            return false;
        }
        private void applyr6(Vertex blank1)
        {
            if (checkr6(blank1))
            {
                Vertex v1 = createVertex(RoomType.Encounter, false);
                Vertex vi = createVertex(RoomType.Item, false);
                connect(blank1, v1, false);
                connect(v1, vi, false);
            }
        }
        private bool checkr7(Vertex blank1, Vertex blankE, Vertex blank2, Vertex CriticalE)
        {
            if (CriticalE.IsCritical)
            {
                if (connected(blank1, blankE) != null)
                {
                    if (connected(blankE, blank2) != null)
                    {
                        if (blankE.Type == RoomType.Encounter)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private void applyr7(Vertex blank1, Vertex blankE, Vertex blank2, Vertex CriticalE)
        {
            if (checkr7(blank1, blankE, blank2, CriticalE))
            {
                blankE.Type = RoomType.Lock;
                Vertex K1 = createVertex(RoomType.Key, false);
                connect(CriticalE, K1, false);
                connect(K1, blankE, true);
            }
        }
        private bool matchr2()
        {
            Vertex startV;
            Vertex goalV;
            List<Vertex> searchspace = Vertices;
            foreach (Vertex vertex in searchspace)
            {
                startV = vertex;
                for (int x = 0; x < Vertices.Count; x++)
                {
                    goalV = searchspace.ElementAt(x);
                    if (startV != goalV)
                    { //checking they're not the same vertex
                        if (checkr2(startV, goalV))
                        {
                            applyr2(startV, goalV);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private bool matchr3()
        {
            Vertex blankV;
            Vertex lockV;
            Vertex goalV;
            List<Vertex> searchspace = Vertices;
            foreach (Vertex vertex in searchspace)
            {
                blankV = vertex;
                for (int x = 0; x < Vertices.Count; x++)
                {
                    lockV = searchspace.ElementAt(x);
                    for (int y = 0; y < Vertices.Count; y++)
                    {
                        goalV = searchspace.ElementAt(x);
                        if ((blankV != lockV) && (goalV != blankV))
                        {
                            if (checkr3(blankV, lockV, goalV))
                            {
                                applyr3(blankV, lockV, goalV);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private bool matchr4()
        {
            Vertex blankS;
            Vertex blankP;
            Vertex blankG;
            List<Vertex> searchspace = Vertices;
            foreach (Vertex vertex in searchspace)
            {
                blankS = vertex;
                for (int x = 0; x < Vertices.Count; x++)
                {
                    blankP = searchspace.ElementAt(x);
                    for (int y = 0; y < Vertices.Count; y++)
                    {
                        blankG = searchspace.ElementAt(x);
                        if ((blankS != blankP) && (blankS != blankG) && (blankP != blankG))
                        {
                            if (checkr4(blankS, blankP, blankG))
                            {
                                applyr4(blankS, blankP, blankG);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private bool matchr5()
        {
            Vertex blank1;
            Vertex blank2;
            List<Vertex> searchspace = Vertices;
            for (int i = 0; i < searchspace.Count; i++)
            {
                blank1 = searchspace.ElementAt(i);
                for (int x = 0; x < Vertices.Count; x++)
                {
                    blank2 = searchspace.ElementAt(x);
                    if (blank1 != blank2)
                    {
                        if (checkr5(blank1, blank2))
                        {
                            applyr5(blank1, blank2);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private bool matchr6()
        { //this match works slightly differently, as it will almost always match with something early in the graph, so some randomness is added
            Random rnd = new Random();
            int numFails = 0;
            Vertex blank1;
            List<Vertex> searchspace = Vertices;
            int chosen = rnd.Next(searchspace.Count);
            blank1 = searchspace.ElementAt(chosen);
            while (!checkr6(blank1))
            {
                chosen = rnd.Next(searchspace.Count);
                blank1 = searchspace.ElementAt(chosen);
                numFails += 1;
                if (numFails >= searchspace.Count)
                { //if there is no matches, return false
                    return false;
                }
            }
            applyr6(blank1);
            return true;
        }
        private bool matchr7()
        {
            Vertex blank1;
            Vertex blankE;
            Vertex blank2;
            Vertex CriticalE;
            List<Vertex> searchspace = Vertices;
            for (int i = 0; i < searchspace.Count; i++)
            {
                blank1 = searchspace.ElementAt(i);
                for (int x = 0; x < Vertices.Count; x++)
                {
                    blankE = searchspace.ElementAt(x);
                    foreach (Vertex vertex in searchspace)
                    {
                        blank2 = vertex;
                        for (int z = 0; z < Vertices.Count; z++)
                        {
                            CriticalE = searchspace.ElementAt(z);
                            if ((blank1 != blank2) && (blank1 != blankE) && (blank1 != CriticalE))
                            {
                                if ((blankE != blank2) && (blankE != CriticalE))
                                {
                                    if (blank2 != CriticalE)
                                    {
                                        applyr7(blank1, blankE, blank2, CriticalE);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

    }
}