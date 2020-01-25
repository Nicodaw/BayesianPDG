using System.Collections.Generic;
using System.Linq;

namespace BayesianPDG.SpaceGenerator.Space
{
    class Node
    {
        public int Id;
        public List<Edge> Edges = new List<Edge>();

        public Node(int id)
        {
            Id = id;
        }

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
    }
}
