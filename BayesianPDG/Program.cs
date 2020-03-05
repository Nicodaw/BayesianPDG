using BayesianPDG.SpaceGenerator;
using BayesianPDG.SpaceGenerator.Space;
using GeneralAlgorithms.DataStructures.Polygons;
using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Interfaces.Core.MapLayouts;
using MapGeneration.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;

namespace BayesianPDG
{
    class Program
    {
        private const string defaultNetPath = "Resources\\BNetworks\\LIEMNet.neta";
        static private double netGenerationTime = 0;
        static private double dunGenerationTime = 0;

        public static void Main()
        {
            Debug.WriteLine("Starting Bayesian Space Generator...");
            BayesianSpaceGenerator spaceGen = new BayesianSpaceGenerator();

            Stopwatch netWatch = new Stopwatch();

            netWatch.Start();
            SpaceGraph graph = spaceGen.RunInference(defaultNetPath, 1000);
            netWatch.Stop();
            netGenerationTime = netWatch.Elapsed.TotalSeconds;

            GenerateMap(graph);

        }

        static void GenerateMap(SpaceGraph graph, int seed = 0)
        {
            //TODO: Finish when graph population is done

            var mapDescription = new MapDescription<int>();

            //Add rooms
            graph.AllNodes.ForEach(node => mapDescription.AddRoom(node.Id));

            //Add connections
            List<List<int>> connections = graph.ConvertToAdjList(false);
            for (int node = 0; node < connections.Count; node++)
            {
                for (int link = 0; link < connections[node].Count; link++)
                {
                    mapDescription.AddPassage(node, connections[node][link]);
                }
            }
            // Add default room shapes
            var doorMode = new OverlapMode(1, 1);

            for (int i = 5; i < 15; i++)
            {
                mapDescription.AddRoomShapes(new RoomDescription(GridPolygon.GetSquare(i), doorMode));
                mapDescription.AddRoomShapes(new RoomDescription(GridPolygon.GetRectangle(i, i + 2), doorMode));
                mapDescription.AddRoomShapes(new RoomDescription(GridPolygon.GetRectangle(i, i + 4), doorMode));
            }


            // Generate bitmap
            SaveBitmap(mapDescription, seed);
        }

        private static void SaveBitmap(MapDescription<int> mapDescription, int seed)
        {
            try
            {
                Stopwatch dunWatch = new Stopwatch();
                dunWatch.Start();
                var layoutGenerator = LayoutGeneratorFactory.GetDefaultChainBasedGenerator<int>();
                layoutGenerator.InjectRandomGenerator(new Random(seed));
                Debug.WriteLine(mapDescription.GetGraph().ToString());
                List<IMapLayout<int>> generatedLayouts = (List<IMapLayout<int>>)layoutGenerator.GetLayouts(mapDescription, 3); //Magic number 3 is how many different layouts we want
                dunWatch.Stop();
                dunGenerationTime = dunWatch.Elapsed.TotalSeconds;
                exportAllJpgButton_Click(generatedLayouts);
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine($"{e.Message} by {e.ParamName} with: {e.Data} {e.StackTrace}");
            }
        }

        private static void exportAllJpgButton_Click(List<IMapLayout<int>> generatedLayouts)
        {
            WFLayoutDrawer<int> wfLayoutDrawer = new WFLayoutDrawer<int>();

            var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            var folder = $"Output/{time}";

            int width = 600;
            int height = 600;

            try
            {
                Directory.CreateDirectory(folder);

                for (var i = 0; i < generatedLayouts.Count; i++)
                {
                    Bitmap bitmap = wfLayoutDrawer.DrawLayout(generatedLayouts[i], width, height, true, null);

                    bitmap.Save($"{folder}/{i}.jpg");
                }
                File.WriteAllText(folder + "/benchmark.txt", $"Inference process took {netGenerationTime}s \n" +
                $"Generation process took {dunGenerationTime}s \n" +
                $"Total elapsed time :: {netGenerationTime + dunGenerationTime}s");

                MessageBox.Show($"Images were saved to {folder}", "Images saved", 0);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.Message} with: {e.Data} {e.StackTrace}");
            }


        }
    }

}
