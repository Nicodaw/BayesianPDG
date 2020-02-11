using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Priority_Queue;

namespace BayesianPDG.SpaceGenerator.Space
{
    public class SpaceGraph
    {
        #region Public Fields
        public List<Node> AllNodes = new List<Node>();
        public Node Entrance => Node(0);
        public Node Goal => Node(AllNodes.Count - 1);
        public List<int> CriticalPath => PathTo(Entrance.Id, Goal.Id);
        public bool isComplete => ValidateNodeConnected() && ValidateReachability();
        public bool isPlanar => AllNodes.GroupBy(node => node.Edges).Count() >= 3 * AllNodes.Count - 6; //Euler's rule for planar graphs: edges <= 3 * vertices - 6;
        public bool areNodesInstantiated => AllNodes.FindAll(x => x.Values.Count == 1).Count == AllNodes.Count; //All room nodes have only 1 value
        #endregion

        #region Constructor
        public SpaceGraph()
        {
            //default
        }
        public SpaceGraph(SpaceGraph other)
        {
            AllNodes = other.AllNodes;
        }
        #endregion

        #region Public Properties
        public Node Node(int id) => AllNodes.First(node => node.Id == id);

        public Node CreateNode(int id)
        {
            var n = new Node(id);
            AllNodes.Add(n);
            return n;
        }

        public void Connect(int parent, int child)
        {
            if (parent != child)
            {
                Node(parent).AddEdge(Node(child));
            }
            else
            {
                Debug.WriteLine($"Parent and child are the same node [{parent}]. Skipping...");
            }

        }

        public void Connect(Node parent, Node child) => Connect(parent.Id, child.Id);

        public void Disconnect(int parent, int child) => Node(parent).RemoveEdge(Node(child));
        public void Disconnect(Node parent, Node child) => Disconnect(parent.Id, child.Id);
        #endregion

        public int?[,] ConvertToAdjMatrix()
        {
            int?[,] adj = new int?[AllNodes.Count, AllNodes.Count];

            for (int i = 0; i < AllNodes.Count; i++)
            {
                Node n1 = Node(i);

                for (int j = 0; j < AllNodes.Count; j++)
                {
                    Node n2 = Node(j);

                    var arc = n1.Edges.FirstOrDefault(a => a.Child == n2);

                    if (arc != null)
                    {
                        adj[i, j] = 1;
                    }
                }
            }
            return adj;
        }
        public List<List<int>> ConvertToAdjList(bool isUndirected = true)
        {
            List<List<int>> adj = new List<List<int>>();
            int?[,] matrix = ConvertToAdjMatrix();
            foreach (Node row in AllNodes)
            {
                adj.Add(new List<int>());
            }
            for (int row = 0; row < AllNodes.Count; row++)
            {
                for (int col = row; col < AllNodes.Count; col++)
                {
                    if (matrix[row, col] != null)
                    {
                        adj[row].Add(col);
                        if (isUndirected) adj[col].Add(row);
                    }
                }
            }

            return adj;
        }

        /// <summary>
        /// Checks if a Critical Path is established and if all nodes are connected
        /// </summary>
        private bool ValidateNodeConnected()
        {
            Debug.WriteLine("[id] => cameFrom");
            CriticalPath.ForEach(node => Debug.WriteLine($"[{CriticalPath.IndexOf(node)}] => {node}"));

            bool isConnected = true;
            foreach (Node node in AllNodes)
            {
                if (node.Edges.Count == 0)
                {
                    isConnected = false;
                    break;
                }
            }
            return CriticalPath.Count > 0 && isConnected;
        }

        /// <summary>
        /// Checks there is a valid path b/w the entrance and each other room
        /// </summary>
        /// <returns>false if it finds at least one dangling room (there could be more)</returns>
        private bool ValidateReachability()
        {
            bool areNodesReachable = true;

            foreach (Node node in AllNodes)
            {
                if (node.Id != Entrance.Id && PathTo(Entrance.Id, node.Id).Count == 0)
                {
                    Debug.WriteLine($"FAILED GRAPH VALIDATION: Non-reachable node {node.Id} detected");
                    areNodesReachable = false;
                    break;
                }
            }
            return areNodesReachable;

        }

        #region Constraints
        /// <summary>
        /// Validate if adding node A to node B will break the invariant
        /// i.e. if it will change the critical path length
        /// </summary>
        /// <param name="A">parent node</param>
        /// <param name="B">child node</param>
        /// <returns>If adding A:B is a valid operation</returns>
        public bool ValidCPLength(Node A, Node B)
        {
            int originalCPLength = CriticalPath.Count;
            Connect(A, B);
            bool isCPValid = CriticalPath.Count == originalCPLength;
            Disconnect(A, B);
            return isCPValid;
        }

        /// <summary>
        /// Assume we've added an edge to A.
        /// Validate if A is still within capacity.
        /// </summary>
        /// <param name="A">node</param>
        /// <returns>If A has not exceeded its neighbour capacity</returns>
        public bool ValidNeighboursPostInc(Node A) => A.Edges.Count < A.MaxNeighbours;
        #endregion


        /// <summary>
        /// Using Dijkstra
        /// </summary>
        /// <param name="A">Start</param>
        /// <param name="B">Goal</param>
        /// <returns>The shortest path b/w the nodes, if it exists. Otherwise null</returns>
        public List<int> PathTo(int A, int B)
        {
            List<List<int>> graphList = ConvertToAdjList();
            SimplePriorityQueue<int> frontier = new SimplePriorityQueue<int>();
            frontier.Enqueue(A, 0);
            List<int?> cameFrom = new List<int?>();
            List<int?> totalCost = new List<int?>();

            graphList.ForEach(_ =>
            {
                cameFrom.Add(null);
                totalCost.Add(null);
            });
            totalCost[A] = 0;

            while (frontier.Count != 0)
            {
                int current = frontier.Dequeue();
                if (current == B) break;

                foreach (int neighbour in graphList[current])
                {
                    int? cost = totalCost[current] + 1;
                    if (totalCost[neighbour] == null || cost < totalCost[neighbour])
                    {
                        totalCost[neighbour] = cost;
                        frontier.Enqueue(neighbour, cost.Value);
                        cameFrom[neighbour] = current;
                    }
                }
            }
            //work your way back to reconstruct the path and remove null nodes
            List<int> reduced = new List<int>();
            if (cameFrom[B].HasValue)
            {
                int? current = B;
                while (current != null)
                {
                    reduced.Add(current.Value);
                    current = cameFrom[current.Value];
                }
                reduced.Reverse();
            }

            return reduced;
        }
        public override string ToString()
        {
            int?[,] matrix = ConvertToAdjMatrix();
            int Count = AllNodes.Count();
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("CP: [{0}]", string.Join(", ", CriticalPath.ToArray()));
            builder.AppendLine();
            builder.Append(' ', 8);
            for (int i = 0; i < Count; i++)
            {
                builder.AppendFormat("{0}", i);
                builder.Append(' ', (i >= 10) ? 1 : 2);
            }

            builder.AppendLine();

            for (int i = 0; i < Count; i++)
            {

                builder.AppendFormat("{0}", i);
                builder.Append(' ', (i >= 10) ? 1 : 2);
                builder.Append("| [ ");

                for (int j = 0; j < Count; j++)
                {
                    if (i == j)
                    {
                        builder.Append(" &,");
                    }
                    else if (matrix[i, j] == null)
                    {
                        builder.Append(" .,");
                    }
                    else
                    {
                        builder.AppendFormat(" {0},", matrix[i, j]);
                    }

                }
                builder.Append(" ]\r\n");
            }
            builder.Append("\r\n");
            // Adjacency list representation
            List<List<int>> list = ConvertToAdjList(false);
            for (int i = 0; i < Count; i++)
            {
                builder.AppendFormat("[{0}]: ", i);

                for (int j = 0; j < list[i].Count; j++)
                {
                    builder.AppendFormat("{0}{1}", list[i][j], j == (list[i].Count - 1) ? "" : " ");
                }
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
