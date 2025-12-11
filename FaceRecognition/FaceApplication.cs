using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceRecognitionApp
{
    public static class FaceApplication
    {
        public static void Run(string readFolderPath, string writeFolderPath)
        {
            if (!Directory.Exists(readFolderPath))
            {
                return;
            }

            Console.WriteLine($"Поиск в папке {readFolderPath} ...");

            var faces = FaceFinder.FindInFolder(readFolderPath);

            if (faces.Count == 0)
            {
                return;
            }

            Console.WriteLine($"Найдено лиц: {faces.Count}");

            var clusters = FaceCluster.FindClusters(faces);

            clusters.Sort((a, b) => b.Faces.Count.CompareTo(a.Faces.Count));

            var exporter = new FaceExporter();

            exporter.Export(clusters, writeFolderPath);
        }
    }
}
