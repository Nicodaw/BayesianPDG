using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Netica;

namespace BayesianPDG.SpaceGenerator
{
    class DAGLoader
    {
        #region Constants
        private const string defaultNetPath = "Resources\\BNetworks\\DungeonNet.neta";
        private const string defaultDataPath = "Resources\\BNData\\dungeon_data.csv";
        #endregion
        private readonly Application _app = BayesianSpaceGenerator.NeticaApp;

        public BNet Net { get; set; }
        public Caseset Data { get; set; }

        public DAGLoader(string netPath = defaultNetPath, string dataPath = defaultDataPath)
        {
            Net = LoadBNet(netPath);
            Data = LoadData(dataPath);
        }

        /// <summary>
        /// Loads the network by instantiating the Net property of the DAGLoader
        /// </summary>
        /// <param name="path">relative path from BaseDirectory</param>
        public BNet LoadBNet(string path)
        {
            Debug.WriteLine($"Loading Bayesian Network from {path}...");
            _app.Visible = true; //Does it need to be visible?
            string net_file_name = AppDomain.CurrentDomain.BaseDirectory + path;
            Streamer file = _app.NewStream(net_file_name, null);
            BNet net = _app.ReadBNet(file);
            net.Compile();
            return net;
        }

        /// <summary>
        /// Loads the data as Netica Casesets by instantiating the Data property of the DAGLoader
        /// </summary>
        /// <param name="path">relative path from BaseDirectory</param>
        /// <param name="name">name of dataset</param>
        public Caseset LoadData(string path, string name = "Dungeon")
        {
            Debug.WriteLine($"Loading Data from {path}...");
            Caseset data = _app.NewCaseset(name);
            string net_file_name = AppDomain.CurrentDomain.BaseDirectory + path;
            Streamer file = _app.NewStream(net_file_name, null);
            data.AddCasesFromFile(file);
            return data;
        }

        public void close()
        {
            Data.Delete();
            Net.Delete();
            if (!_app.UserControl) _app.Quit();
            Debug.WriteLine("Press <enter> to quit.");
            Console.ReadLine();
        }
    }
}