using System;

namespace BayesianPDG.SpaceGenerator.Space
{
    public class Edge : IEquatable<Edge>
    {
        public Node Parent;
        public Node Child;

        public bool Equals(Edge other)
        {
            return (Parent.Equals(other.Parent) && Child.Equals(other.Child));
        }
    }
}
