using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BayesianPDG.SpaceGenerator.Space
{
    public class Node
    {
        #region Public Fields
        public int Id;
        public List<Edge> Edges = new List<Edge>();
        public bool IsConnected(Node other) => Edges.Find(edge => edge.Child == other || edge.Parent == other) != null;
        #endregion

        #region Constraints
        public int? CPDistance; // 0 is on the Critical Path
        public int? MaxNeighbours;
        public int? Depth;
        #endregion

        #region CSP Definitions
        /// <summary>
        /// The potential candidates for connection.
        /// Must satisfy
        /// =============
        /// Cardinality :: Values.Count == MaxNeighbours
        /// Ordering    :: Node[Entrance] > Node[1] ... Node[Goal] && Node[Goal] < Node[Goal -1] ... Node[Entrance]
        /// Relationship:: Node[i]::Node[j] must not change the CPLength for any i and j
        /// Relationship:: Graph must be fully connected, isReachable(Node[Entrance],Node[i]) for any i
        /// Functional  :: Graph must be planar. Euler's method must hold as invariant
        /// </summary>
        public List<List<Node>> Values;
        #endregion

        #region Constructor
        public Node(int id)
        {
            if (id == 0)
            {
                CPDistance = 0;
                Depth = 0;
            }
            Id = id;
            Values = new List<List<Node>>();
        }
        public Node(Node other)
        {
            Id = other.Id;
            Edges = other.Edges;
            CPDistance = other.CPDistance;
            MaxNeighbours = other.MaxNeighbours;
            Depth = other.Depth;
            Values = other.Values;
        }
        public Node() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a new edge
        /// Also, it creates the inversed node in the passed node
        /// </summary>
        /// <returns>this</returns>
        public Node AddEdge(Node child)
        {
            if (!this.Edges.Exists(e => e.Parent == this && e.Child == child))
            {
                Edges.Add(new Edge
                {
                    Parent = this,
                    Child = child
                });

                if (!child.Edges.Exists(e => e.Parent == child && e.Child == this))
                {
                    child.AddEdge(this);
                }

                return this;
            }
            else
            {
                // Debug.WriteLine($"Edge b/w [{this.Id}:{child.Id}] already added, skipping...");
                return this;
            }

        }
        /// <summary>
        /// Delete an existing edge
        /// Also, it deletes the inversed node in the passed node
        /// </summary>
        /// <returns>this</returns>
        public Node RemoveEdge(Node child)
        {
            if (Edges.Exists(e => e.Parent == this && e.Child == child))
            {
                Edges.Remove(Edges.First<Edge>(e => e.Parent == this && e.Child == child));
            }
            if (child.Edges.Exists(e => e.Parent == child && e.Child == this))
            {
                child.RemoveEdge(this);
            }
            return this;
        }
        #endregion

        public string PrintConnections()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"{this.Values.Count}x{this.MaxNeighbours} for room {this.Id}::");
            foreach (List<Node> set in Values)
            {
                builder.Append('[');
                builder.AppendFormat("{0}", string.Join(", ", set.Select(x => x.Id).ToList()));
                builder.Append(']');
            }

            return builder.ToString();
        }
    }
}
