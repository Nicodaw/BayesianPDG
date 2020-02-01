using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BayesianPDG.SpaceGenerator.Space
{
    class SpaceGraph
    {
        #region Public Fields
        public List<Node> AllNodes = new List<Node>();
        public Node Entrance => Node(0);
        public Node Goal => Node(AllNodes.Count - 1);
        public bool isComplete => ValidateGraph();
        public List<List<int>> GraphList => ConvertToAdjList();
        #endregion
        #region Public Properties
        public Node Node(int id) => AllNodes.First(node => node.Id == id);

        public Node CreateNode(int id)
        {
            var n = new Node(id);
            AllNodes.Add(n);
            return n;
        }

        public void Connect(int parent, int child) => Node(parent).AddEdge(Node(child));

        public void Disconnect(int parent, int child) => Node(parent).RemoveEdge(Node(child));
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
        public List<List<int>> ConvertToAdjList()
        { 
            List<List<int>> adj = new List<List<int>>();
            int?[,] matrix = ConvertToAdjMatrix();
            for (int row = 0; row < AllNodes.Count; row++)
            {
                adj.Add(new List<int>());
                for (int col = row; col < AllNodes.Count; col++)
                {
                    if (matrix[row, col] != null)
                    {
                        adj[row].Add(col);
                    }
                }
            }
            return adj;
        }

        private bool ValidateGraph()
        {
            // is there a path from Entrance to G
            List<int?> pathEG = PathTo(Entrance.Id, Goal.Id);
            pathEG.ForEach(node => Debug.WriteLine($"path node {node}"));

            bool isConnected = true;
            foreach (Node node in AllNodes)
            {
                if (node.Edges.Count == 0) isConnected = false;
            }


            return pathEG.Any(_ => _ != null) && isConnected;
        }

        /// <summary>
        /// Using Breath First + early exit
        /// </summary>
        /// <param name="A">Start</param>
        /// <param name="B">Goal</param>
        /// <returns>The shortest path b/w the nodes, if it exists. Otherwise null</returns>
        private List<int?> PathTo(int A, int B)
        {
            Queue<int> frontier = new Queue<int>();
            frontier.Enqueue(A);
            List<int?> cameFrom = new List<int?>();
            GraphList.ForEach(_ => cameFrom.Add(null));

            while (frontier.Count != 0)
            {
                int current = frontier.Dequeue();
                if (current == B) break;

                foreach (int neighbour in GraphList[current])
                {
                    if (!cameFrom.Contains(neighbour))
                    {
                        frontier.Enqueue(neighbour);
                        cameFrom[neighbour] = current;
                    }
                }
            }
            return cameFrom;


        }
        public override string ToString()
        {
            int?[,] matrix = ConvertToAdjMatrix();
            int Count = AllNodes.Count();
            StringBuilder builder = new StringBuilder();
            builder.Append(' ', 7);
            for (int i = 0; i < Count; i++)
            {
                builder.AppendFormat("{0}  ", i);
            }

            builder.AppendLine();

            for (int i = 0; i < Count; i++)
            {
                builder.AppendFormat("{0} | [ ", i);

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
            List<List<int>> list = ConvertToAdjList();
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
