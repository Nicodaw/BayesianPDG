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
        public static void Main()
        {
            Debug.WriteLine("Starting Bayesian Space Generator...");
            BayesianSpaceGenerator spaceGen = new BayesianSpaceGenerator();
            SpaceGraph graph = spaceGen.RunInference(1000);
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
            var squareRoom = new RoomDescription(
              GridPolygon.GetSquare(8),
              doorMode
            );
            var rectangleRoom = new RoomDescription(
              GridPolygon.GetRectangle(6, 10),
              doorMode
            );
            mapDescription.AddRoomShapes(squareRoom);
            mapDescription.AddRoomShapes(rectangleRoom);

            // Generate bitmap
            SaveBitmap(mapDescription, seed);
        }

        private static void SaveBitmap(MapDescription<int> mapDescription, int seed)
        {
            try
            {
                var layoutGenerator = LayoutGeneratorFactory.GetDefaultChainBasedGenerator<int>();
                layoutGenerator.InjectRandomGenerator(new Random(seed));
                Debug.WriteLine(mapDescription.GetGraph().ToString());
                List<IMapLayout<int>> generatedLayouts = (List<IMapLayout<int>>)layoutGenerator.GetLayouts(mapDescription, 3); //Magic number 3 is how many different layouts we want
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

            Directory.CreateDirectory(folder);

            for (var i = 0; i < generatedLayouts.Count; i++)
            {
                Bitmap bitmap = wfLayoutDrawer.DrawLayout(generatedLayouts[i], width, height, true, null);

                bitmap.Save($"{folder}/{i}.jpg");
            }

            MessageBox.Show($"Images were saved to {folder}", "Images saved", 0);
        }
    }

}
