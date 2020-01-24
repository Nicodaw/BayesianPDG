using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BayesianPDG.SpaceGenerator.Space
{
    class SpaceGraph
    {
        public Node Root;
        public List<Node> AllNodes = new List<Node>();
        public Node Node(int id) => AllNodes.First<Node>(node => node.Id == id);

        public Node CreateNode(int id)
        {
            var n = new Node(id);
            AllNodes.Add(n);
            return n;
        }

        public void Connect(int parent, int child)
        {
            Node(parent).AddEdge(Node(child));
        }

        private int?[,] ConvertToAdjMatrix()
        {
            int?[,] adj = new int?[AllNodes.Count, AllNodes.Count];

            for (int i = 0; i < AllNodes.Count; i++)
            {
                Node n1 = AllNodes[i];

                for (int j = 0; j < AllNodes.Count; j++)
                {
                    Node n2 = AllNodes[j];

                    var arc = n1.Edges.FirstOrDefault(a => a.Child == n2);

                    if (arc != null)
                    {
                        adj[i, j] = 1;
                    }
                }
            }
            return adj;
        }
        private List<List<int>> ConvertToAdjList()
        {   //ToDo: implement Adjacency lists ...
            //flatten down adj matrix
            List<List<int>> adj = new List<List<int>>();
            int?[,] matrix = ConvertToAdjMatrix();
            for (int row = 0; row < AllNodes.Count; row++)
            {
                adj.Add(new List<int>());
                for (int col = row; col < AllNodes.Count; col++)
                {
                    if (matrix[row,col] != null)
                    {
                        adj[row].Add(col);
                    }
                }
            }
            return adj;
        }
        public override string ToString()
        {
            int?[,] matrix = ConvertToAdjMatrix();
            int Count = AllNodes.Count();
            StringBuilder builder = new StringBuilder();
            builder.Append(' ',7);
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
