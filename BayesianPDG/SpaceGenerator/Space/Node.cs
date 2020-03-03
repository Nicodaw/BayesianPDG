using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BayesianPDG.SpaceGenerator.Space
{
    public class Node: IEquatable<Node>
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
            if (!this.Edges.Exists(e => e.Parent.Id == Id && e.Child.Id == child.Id))
            {
                Edges.Add(new Edge
                {
                    Parent = this,
                    Child = child
                });

                if (!child.Edges.Exists(e => e.Parent.Id == child.Id && e.Child.Id == Id))
                {
                    child.AddEdge(this);
                }

                return this;
            }
            else
            {
                Debug.WriteLine($"Edge b/w [{Id}:{child.Id}] already added, skipping...");
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
            builder.Append($"{Values.Count}x{MaxNeighbours} for room {Id}::");
            foreach (List<Node> set in Values)
            {
                builder.Append('[');
                builder.AppendFormat("{0}", string.Join(", ", set.Select(x => x.Id).ToList()));
                builder.Append(']');
            }

            return builder.ToString();
        }

        public bool ValuesEqual(Node other) => ValuesEqual(other.Values);

        public bool ValuesEqual(List<List<Node>> otherValues)
        {
            if (Values.Count != otherValues.Count)
                return false;
            else
            {
                for (int i = 0; i < Values.Count; i++)
                {
                    for (int j = 0; j < Values[i].Count; j++)
                    {
                        if (!Values[i].Contains(otherValues[i][j]))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        public bool Equals(Node other)
        {
            return Id == other.Id;
        }
    }
}
