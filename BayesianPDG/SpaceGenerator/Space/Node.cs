using System.Collections.Generic;
using System.Linq;

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

        #region Constructor
        public Node(int id)
        {
            if (id == 0)
            {
                CPDistance = 0;
                Depth = 0;
            }
            Id = id;
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
    }
}
