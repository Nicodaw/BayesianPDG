using BayesianPDG.MissionGenerator;
using System;
using System.Collections.Generic;

namespace MissionGenerator
{
    class Vertex
    {
        #region Private Properties
        bool _isCritical;
        int _id;
        RoomType _roomType;
        List<Edge> _in;
        List<Edge> _out;
        #endregion

        #region Public Properties
        public bool IsCritical => _isCritical;
        public int Id => _id;
        public RoomType Type { get => _roomType; set => _roomType = value; }
        public List<Edge> Incoming => _in;
        public List<Edge> Outgoing => _out;
        #endregion

        #region Constructor
        public Vertex(int id, bool isCritical, RoomType roomType)
        {
            _id = id;
            _isCritical = isCritical;
            _roomType = roomType;
            _in = new List<Edge>();
            _out = new List<Edge>();
        }
        #endregion

        #region Methods
        public bool AddIncoming(Edge e) 
        {
            if (Incoming.Contains(e)) return false;
            else
            {
                e.Target = this;
                Incoming.Add(e);
                return true;
            }
        }

        public bool RemoveIncoming(Edge e)
        {
            if (Incoming.Contains(e))
            {
                e.Target = null;
                Incoming.Remove(e);
                return true;
            }
            else return false;
        }

        public bool AddOutgoing(Edge e)
        {
            if (Outgoing.Contains(e)) return false;
            else
            {
                e.Source = this;
                Outgoing.Add(e);
                return true;
            }
        }

        public bool RemoveOutgoing(Edge e)
        {
            if (Outgoing.Contains(e))
            {
                e.Source = null;
                Outgoing.Remove(e);
                return true;
            }
            else return false;
        }
        #endregion
    }
}
