using MissionGenerator;
using System;
using System.Diagnostics;

namespace BayesianPDG.MissionGenerator
{
    class Edge
    {
        public Vertex Source { get; set; }
        public Vertex Target { get; set; }
        public Boolean HasKey { get; set; }
        public int Id { get; set; }


        public Edge(int id, bool hasKey = false)
        {
            Id = id;
            HasKey = hasKey;
        }

        public Edge(Edge copy)
        {
            Id = copy.Id;
            HasKey = copy.HasKey;
            Source = copy.Source;
            Target = copy.Target;
        }

        //public bool Equals(Edge other)
        //{
        //    if (Id == other.Id)
        //    {
        //        return true;
        //    }
        //    else if ((Source.Id == other.Source?.Id && Target.Id == other.Target?.Id) ||
        //            (Source.Id == other.Target?.Id && Target.Id == other.Source?.Id))
        //    {
        //        return true;
        //    }
        //    else return false;
        //}

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
