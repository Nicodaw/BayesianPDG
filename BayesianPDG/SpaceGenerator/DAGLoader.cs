using System;
using System.Diagnostics;
using Netica;

namespace BayesianPDG.SpaceGenerator
{
    class DAGLoader
    {
        #region Constants
        #endregion
        private readonly Application _app;

        public BNet Net { get; set; }

        public DAGLoader(string netPath, Application context)
        {
            _app = context;
            Net = LoadBNet(netPath);
        }

        /// <summary>
        /// Loads the network by instantiating the Net property of the DAGLoader
        /// </summary>
        /// <param name="path">relative path from BaseDirectory</param>
        public BNet LoadBNet(string path)
        {
            Debug.WriteLine($"Loading Bayesian Network from {path}...");
            _app.Visible = false;
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
            try
            {
                Net.Delete();
                if (!_app.UserControl) _app.Quit();
                //Debug.WriteLine("Press <enter> to quit.");
                //Console.ReadLine();
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                Debug.WriteLine("Net Already closed");
            }
            
        }
    }
}