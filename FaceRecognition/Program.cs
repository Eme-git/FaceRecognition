using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace FaceRecognitionApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string photosFolder = "D:\\Projects\\Visual Studio\\FaceRecognition\\FaceRecognition\\images";

            if (!Directory.Exists(photosFolder))
            {
                Console.WriteLine($"Папка не найдена: {photosFolder}");
                return;
            }

            Console.WriteLine($"Поиск в папке {photosFolder} ...");

            var faces = FaceFinder.FindInFolder(photosFolder);

            if (faces.Count == 0)
            {
                Console.WriteLine("Нет лиц для обработки");
                return;
            }

            Console.WriteLine($"Найдено лиц: {faces.Count}");

            // Папка для визуализации
            string vizFolder = Path.Combine(photosFolder, "visualized");

            var clusters = FaceCluster.FindClusters(faces);

            var exporter = new FaceExporter();

            exporter.Export(clusters, vizFolder);

            // Открываем папку с результатами
            OpenResultsFolder(vizFolder);
        }

        static void OpenResultsFolder(string folder)
        {
            Console.Write("\nОткрыть папку с результатами? (y/n): ");
            var response = Console.ReadLine()?.ToLower();

            if (response == "y" || response == "д")
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", folder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Не удалось открыть папку: {ex.Message}");
                }
            }
        }
    }
}