﻿using BayesianPDG.SpaceGenerator;
using BayesianPDG.SpaceGenerator.Space;
using GeneralAlgorithms.DataStructures.Polygons;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Interfaces.Core.MapLayouts;
using MapGeneration.Utils;
using MapGeneration.Utils.ConfigParsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using BayesianPDG.UI;
using System.Windows.Forms;

namespace BayesianPDG
{
    class Program
    {
        private const bool enableUserInput = true;
        private const string defaultNetPath = "Resources\\BNetworks\\LIEMNet.neta";
        static private ConfigLoader cfloader = new ConfigLoader();
        static private double netGenerationTime = 0;
        static private double dunGenerationTime = 0;

        public static void Main()
        {
            Debug.WriteLine("Starting Bayesian Space Generator...");

            if (enableUserInput)
            {
                BayesianSpaceGenerator spaceGen = new BayesianSpaceGenerator();
                string value = "10";
                if (Dialog.InputBox("Bayesian PDG", "Enter a Dungeon size between 2-27:", ref value) == DialogResult.OK)
                {
                    SpaceGraph graph = spaceGen.RunInference(Int32.Parse(value), defaultNetPath);
                    if (graph != null)
                    {
                        GenerateMap(graph);
                    }
                    else
                    {
                        MessageBox.Show("Constraints imposed by the sample could not be satisfied. Try again.", "Error: Bad Sample", 0);
                    }
                }
            }
            else
            {
                for (int i = 2; i < 27; i++)
                {
                    Stopwatch netWatch = new Stopwatch();
                    BayesianSpaceGenerator spaceGen = new BayesianSpaceGenerator();
                    netWatch.Start();
                    SpaceGraph experimentGraph = spaceGen.RunInference(i, defaultNetPath);
                    netWatch.Stop();

                    if (experimentGraph != null)
                    {
                        netGenerationTime = netWatch.Elapsed.TotalSeconds;
                        GenerateMap(experimentGraph);
                    }
                }
            }

            //FileInfo[] files = new DirectoryInfo("Resources\\Maps").GetFiles("*.yaml");
            //foreach (var map in files)
            //{
            //    GenerateStaticMap(map.Name);
            //}
        }

        static void GenerateStaticMap(string mapName)
        {
            Debug.WriteLine($"Now saving {mapName}");
            var mapDescription = cfloader.LoadMapDescriptionFromResources(mapName);
            SaveBitmap(mapDescription, 784864, mapName);
        }


        static void GenerateMap(SpaceGraph graph, int seed = 0)
        {
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
            //Add room descriptions
            using (var reader = new StreamReader(@"Resources\Rooms\SMB.yml"))
            {
                var roomLoader = cfloader.LoadRoomDescriptionsSetModel(reader);
                foreach (var roomDescription in roomLoader.RoomDescriptions)
                {
                    GridPolygon shape = new GridPolygon(roomDescription.Value.Shape);
                    RoomDescription roomShape = new RoomDescription(shape, (roomDescription.Value.DoorMode == null) ? roomLoader.Default.DoorMode : roomDescription.Value.DoorMode);
                    mapDescription.AddRoomShapes(roomShape);
                }
            }

            // Generate bitmap
            SaveBitmap(mapDescription, seed);
        }

        private static void SaveBitmap(MapDescription<int> mapDescription, int seed, string name = null)
        {
            try
            {
                Stopwatch dunWatch = new Stopwatch();
                dunWatch.Start();
                var layoutGenerator = LayoutGeneratorFactory.GetDefaultChainBasedGenerator<int>();
                layoutGenerator.InjectRandomGenerator(new Random(seed));
                Debug.WriteLine(mapDescription.GetGraph().ToString());
                List<IMapLayout<int>> generatedLayouts = (List<IMapLayout<int>>)layoutGenerator.GetLayouts(mapDescription, 10); //Magic number 3 is how many different layouts we want
                dunWatch.Stop();
                dunGenerationTime = dunWatch.Elapsed.TotalSeconds;
                exportAllJpgButton_Click(generatedLayouts, name);
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine($"{e.Message} by {e.ParamName} with: {e.Data} {e.StackTrace}");
            }
        }

        private static void exportAllJpgButton_Click(List<IMapLayout<int>> generatedLayouts, string name = null)
        {
            WFLayoutDrawer<int> wfLayoutDrawer = new WFLayoutDrawer<int>();

            var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            if (generatedLayouts.Count == 0)
            {
                throw new ArgumentNullException("No generator layouts were produced");
            }
            var folder = $"Output/{time}_{name ?? generatedLayouts.First().Rooms.Count().ToString()}";
            int width = 600;
            int height = 600;
            try
            {
                Directory.CreateDirectory(folder);

                for (var i = 0; i < generatedLayouts.Count; i++)
                {
                    Bitmap bitmap = wfLayoutDrawer.DrawLayout(generatedLayouts[i], width, height, true, null);

                    bitmap.Save($"{folder}/{name + "_" ?? ""}{i}.jpg");
                }
                File.WriteAllText(folder + "/benchmark.txt", $"Inference process took {netGenerationTime}s \n" +
                $"Generation process took {dunGenerationTime}s \n" +
                $"Total elapsed time :: {netGenerationTime + dunGenerationTime}s");

                if (enableUserInput)
                {
                    string output = new DirectoryInfo(folder).FullName;
                    MessageBox.Show($"Images were saved to {output} \n Press Ctrl + C to copy the directory", "Images saved", 0);
                    Console.Write("Press any key to exit...");
                    Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.Message} with: {e.Data} {e.StackTrace}");
            }


        }
    }

}
