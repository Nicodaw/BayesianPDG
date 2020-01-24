using BayesianPDG.MissionGenerator;
using BayesianPDG.SpaceGenerator;
using BayesianPDG.SpaceGenerator.Space;
using GeneralAlgorithms.DataStructures.Polygons;
using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.LayoutGenerators;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Interfaces.Core.MapLayouts;
using MapGeneration.Utils;
using MissionGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using Edge = BayesianPDG.MissionGenerator.Edge;

namespace BayesianPDG
{
    class Program
    {
        public static void Main()
        {
            Debug.WriteLine("Starting Bayesian Space Generator...");
            BayesianSpaceGenerator spaceGen = new BayesianSpaceGenerator();
            SpaceGraph graph = spaceGen.RunInference();
            GenerateMap(graph);

            //Mission mg = new Mission();
            //mg.GenerateDungeon(2, 2);
            //PrintMissionGraph(mg);
            //GenerateMap(mg.Vertices, mg.Edges);
        }

        static void GenerateMap(List<Vertex> rooms, List<Edge> corridors)
        {
            var layoutGenerator = LayoutGeneratorFactory.GetDefaultChainBasedGenerator<int>();
            layoutGenerator.InjectRandomGenerator(new Random(0));

            var mapDescription = new MapDescription<int>();

            // List<Edge> clear_corr = corridorValidator(corridors);
            foreach (Vertex room in rooms)
            {
                Debug.WriteLine($"Adding {(room.IsCritical ? "Critical " : "")}Room: ({room.Id}, {room.Type})...");
                mapDescription.AddRoom(room.Id);
            }

            List<Edge> connected = new List<Edge>();

            foreach (Edge corridor in corridors)
            {
                if (connected.Find(cor => (cor.Source.Id == corridor.Source.Id) && (cor.Target.Id == corridor.Target.Id)) == null)
                {
                    connected.Add(corridor);
                    mapDescription.AddPassage(corridor.Source.Id, corridor.Target.Id);
                    Edge inverse = new Edge(corridor);
                    inverse.Source = corridor.Target;
                    inverse.Target = corridor.Source;
                    connected.Add(inverse);
                    Debug.WriteLine($"Adding Edge: ({corridor.Source.Id}:{corridor.Target.Id})...");
                }
                else
                {
                    Debug.WriteLine($"({corridor.Source.Id}:{corridor.Target.Id}) is already connected! skipping...");
                }
            }

            // Add room shapes
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

            try
            {
                Debug.WriteLine(mapDescription.GetGraph().ToString());
                List<IMapLayout<int>> generatedLayouts = (List<IMapLayout<int>>)layoutGenerator.GetLayouts(mapDescription, 3);
                exportAllJpgButton_Click(generatedLayouts);
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine($"{e.Message} by {e.ParamName} with: {e.Data} {e.StackTrace}");
            }

        }

        static void GenerateMap(SpaceGraph graph, int seed = 0)
        {
            //TODO: Finish when graph population is done

            var mapDescription = new MapDescription<int>();

            //Add rooms
            graph.AllNodes.ForEach(node => mapDescription.AddRoom(node.Id));
            //Add connections
            List<List<int>> connections = graph.ConvertToAdjList();
            for (int i = 0; i < connections.Count; i++)
            {
                for (int j = 0; j < connections[i].Count; j++)
                {
                    mapDescription.AddPassage(i, connections[i][j]);
                }
            }
            // Add room shapes
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

        private static List<Edge> corridorValidator(List<Edge> corridors)
        {
            for (int i = 0; i < corridors.Count; i++)
            {
                for (int j = 0; j < corridors.Count; j++)
                {
                    Edge e1 = corridors.ElementAt(i);
                    Edge e2 = corridors.ElementAt(j);
                    if ((e1.Source.Id == e2.Target.Id) &&
                        (e2.Source.Id == e1.Target.Id))
                    {
                        Debug.WriteLine($"MATCH FOUND: ({e1.Source.Id}:{e1.Target.Id}) vs ({e2.Source.Id}:{e2.Target.Id})");
                        corridors.Remove(e2);
                    };
                }
            }
            return corridors;
        }

        private static void PrintMissionGraph(Mission mission)
        {
            foreach (Vertex v in mission.Vertices)
            {
                System.Diagnostics.Debug.WriteLine("V" + v.Id + " is type: " + v.Type);
                foreach (Edge e in v.Incoming)
                {
                    System.Diagnostics.Debug.WriteLine("Incoming edge (" + e.Source.Id + ", " + e.Target.Id + ")");
                }
                foreach (Edge e in v.Outgoing)
                {
                    System.Diagnostics.Debug.WriteLine("Outgoing edge (" + e.Source.Id + ", " + e.Target.Id + ")");
                }
            }
            foreach (Edge e in mission.Edges)
            {
                System.Diagnostics.Debug.WriteLine("E" + e.Id + " with Source and Target: " + e.Source.Id + "," + e.Target.Id);
            }
        }


    }

}
