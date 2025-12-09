using FaceRecognitionDotNet;

namespace FaceRecognitionApp
{
    public static class FaceFinder
    {
        private static readonly FaceRecognition _faceRecognition = FaceRecognition.Create("models");

        public static void FindInFile(string imagePath, List<FaceInfo> faces)
        {
            if (!File.Exists(imagePath)) return;

            try
            {
                using var image = FaceRecognition.LoadImageFile(imagePath);
                // Ищем лица
                var locations = _faceRecognition.FaceLocations(image).ToArray();

                if (locations == null || locations.Count() == 0)
                {
                    return;
                }

                // Находим отпечаток
                var encodings = _faceRecognition.FaceEncodings(image, locations).ToArray();

                // Сохраняем данные
                for (int i = 0; i < encodings.Count(); i++)
                {
                    var face = new FaceInfo(imagePath, locations[i], encodings[i]);

                    faces.Add(face);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Ошибка: {ex.Message}");
            }
        }

        public static List<FaceInfo> FindInFolder(string folderPath)
        {
            var result = new List<FaceInfo>();

            var imagePaths = GetImageFiles(folderPath);

            foreach (var imagePath in imagePaths) 
            { 
                FindInFile(imagePath, result);
            }

            return result;
        }

        public static string[] GetImageFiles(string folderPath)
        {
            return Directory.GetFiles(folderPath, "*.jpg")
                           .Concat(Directory.GetFiles(folderPath, "*.jpeg"))
                           .Concat(Directory.GetFiles(folderPath, "*.png"))
                           .Concat(Directory.GetFiles(folderPath, "*.bmp"))
                           .ToArray();
        }
    }
}
