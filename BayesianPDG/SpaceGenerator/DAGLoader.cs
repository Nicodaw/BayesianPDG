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
        private readonly Application _app = new Application();

        public BNet Net { get; set; }

        public DAGLoader(String path = "Resources\\BNetworks\\DungeonNet.neta")
        {
            Debug.WriteLine($"Loading Bayesian Network from {path}");

            _app.Visible = true;

            string net_file_name = AppDomain.CurrentDomain.BaseDirectory + path;

            Streamer file = _app.NewStream(net_file_name, null);

            Net = _app.ReadBNet(file, "");

            Net.Compile();
        }

        public void close()
        {
            Net.Delete();

            if (!_app.UserControl) _app.Quit();
            Debug.WriteLine("Press <enter> to quit.");

            Console.ReadLine();
        }
    }
}