using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Create a new arc, connecting this Node to the Node passed in the parameter
        /// Also, it creates the inversed node in the passed node
        /// </summary>
        public Node AddEdge(Node child)
        {
            Edges.Add(new Edge
            {
                Parent = this,
                Child = child
            });

            if (!child.Edges.Exists(a => a.Parent == child && a.Child == this))
            {
                child.AddEdge(this);
            }

            return this;
        }

        public override bool Equals(object obj)
        {
            if(obj.GetType() == typeof(Node))
            {
                Node other =  (Node) obj;
                if(other.Id == Id)
                {
                    return true;
                } 
                //TODO Check for edge content comparison
            }
            return false;
        }
    }
}
