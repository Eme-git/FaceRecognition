using FaceRecognitionDotNet;

namespace FaceRecognitionApp
{
    public static class FaceFinder
    {
        public static void FindInFile(string imagePath, List<FaceInfo> faces)
        {
            if (!File.Exists(imagePath)) return;

            try
            {
                FaceRecognition faceRecognition = FaceRecognition.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models"));
                
                using var image = FaceRecognition.LoadImageFile(imagePath);
                // Ищем лица
                var locations = faceRecognition.FaceLocations(image).ToArray();

                if (locations == null || locations.Count() == 0)
                {
                    return;
                }

                // Находим отпечаток
                var encodings = faceRecognition.FaceEncodings(image, locations).ToArray();

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
            string[] patterns = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };

            var imageFiles = new List<string>();

            foreach (var pattern in patterns)
            {
                try
                {
                    imageFiles.AddRange(Directory.GetFiles(folderPath, pattern));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка поиска {pattern}: {ex.Message}");
                }
            }

            return imageFiles.Distinct().ToArray();
        }
    }
}
