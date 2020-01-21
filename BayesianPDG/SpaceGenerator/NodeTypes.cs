using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BayesianPDG.SpaceGenerator
{
    /// <summary>
    /// The names of our parameters/features
    /// Each one is a unique node in the DAG 
    /// </summary>
    enum NodeTypes
    {
        NumRooms,
        CriticalPathLength,
        NumDoors,
        Depth,
        CriticalPathDistance,
        NumNeighbours
    }
}
