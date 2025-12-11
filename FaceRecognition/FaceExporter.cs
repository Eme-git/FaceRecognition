
using System;
using System.Collections.Generic;
using System.Drawing; 
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using FaceRecognitionDotNet;


using DrawingImage = System.Drawing.Image;

namespace FaceRecognitionApp
{
    public class FaceExporter
    {
        // Коэффициент увеличения области вокруг лица (10% = 1.1)
        private readonly float _paddingFactor;
        // Делать ли область квадратной
        private readonly bool _makeSquare;
        // Минимальный размер выходного изображения
        private readonly int _minOutputSize;

        public FaceExporter(float paddingFactor = 1.1f, bool makeSquare = true, int minOutputSize = 100)
        {
            _paddingFactor = Math.Max(1.0f, paddingFactor); // Не менее 1.0
            _makeSquare = makeSquare;
            _minOutputSize = Math.Max(50, minOutputSize); // Не менее 50px
        }

        // Основной метод: обрезать лицо и сохранить
        public string Export(FaceInfo faceInfo, string outputFolder,
                                     string fileNamePrefix = "face")
        {
            if (!File.Exists(faceInfo.ImagePath))
                throw new FileNotFoundException($"Файл не найден: {faceInfo.ImagePath}");

            try
            {
                // Загружаем оригинальное изображение
                using var originalImage = DrawingImage.FromFile(faceInfo.ImagePath);

                // Вычисляем область для обрезки с padding
                var cropRect = CalculateCropRectangle(faceInfo.Location, originalImage.Size);

                // Проверяем, что область в пределах изображения
                cropRect = ValidateCropRectangle(cropRect, originalImage.Size);

                // Создаем обрезанное изображение
                using var croppedImage = CropImage(originalImage, cropRect);

                // Создаем имя файла
                string fileName = GenerateFileName(faceInfo, fileNamePrefix);
                string outputPath = Path.Combine(outputFolder, fileName);

                // Создаем папку если нет
                Directory.CreateDirectory(outputFolder);

                // Сохраняем изображение
                SaveImage(croppedImage, outputPath);

                return outputPath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Ошибка обрезки лица из {faceInfo.ImagePath}: {ex.Message}", ex);
            }
        }

        public void Export(List<FaceCluster> clusters, string outputFolder)
        {
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }
            Directory.CreateDirectory(outputFolder);

            for (int i = 0; i < clusters.Count; ++i)
            {
                string outputPath = Path.Combine(outputFolder, $"Person {i + 1}");
                Directory.CreateDirectory(outputPath);

                foreach (var face in clusters[i].Faces)
                {
                    Export(face, outputPath);
                }
            }
        }

        // Вычисление прямоугольника для обрезки с padding
        private Rectangle CalculateCropRectangle(Location faceLocation, Size imageSize)
        {
            // Преобразуем Location в Rectangle
            var faceRect = new Rectangle(
                faceLocation.Left,
                faceLocation.Top,
                faceLocation.Right - faceLocation.Left,
                faceLocation.Bottom - faceLocation.Top);

            // Вычисляем центр лица
            int centerX = faceRect.X + faceRect.Width / 2;
            int centerY = faceRect.Y + faceRect.Height / 2;

            // Вычисляем размер с учетом padding
            int paddedWidth = (int)(faceRect.Width * _paddingFactor);
            int paddedHeight = (int)(faceRect.Height * _paddingFactor);

            // Если нужно сделать квадрат - берем максимальный размер
            if (_makeSquare)
            {
                int maxSize = Math.Max(paddedWidth, paddedHeight);
                paddedWidth = maxSize;
                paddedHeight = maxSize;
            }

            // Убеждаемся, что размер не меньше минимального
            paddedWidth = Math.Max(paddedWidth, _minOutputSize);
            paddedHeight = Math.Max(paddedHeight, _minOutputSize);

            // Вычисляем координаты для центрирования
            int x = centerX - paddedWidth / 2;
            int y = centerY - paddedHeight / 2;

            return new Rectangle(x, y, paddedWidth, paddedHeight);
        }

        // Проверка и корректировка прямоугольника обрезки
        private Rectangle ValidateCropRectangle(Rectangle rect, Size imageSize)
        {
            // Проверяем границы
            int x = Math.Max(0, rect.X);
            int y = Math.Max(0, rect.Y);

            // Если выходим за правую/нижнюю границу
            if (x + rect.Width > imageSize.Width)
                x = imageSize.Width - rect.Width;

            if (y + rect.Height > imageSize.Height)
                y = imageSize.Height - rect.Height;

            // Если после корректировки x/y стали отрицательными
            x = Math.Max(0, x);
            y = Math.Max(0, y);

            // Корректируем размер если все еще выходим за границы
            int width = rect.Width;
            int height = rect.Height;

            if (x + width > imageSize.Width)
                width = imageSize.Width - x;

            if (y + height > imageSize.Height)
                height = imageSize.Height - y;

            // Гарантируем минимальный размер
            width = Math.Max(width, _minOutputSize);
            height = Math.Max(height, _minOutputSize);

            return new Rectangle(x, y, width, height);
        }

        // Обрезка изображения
        private DrawingImage CropImage(DrawingImage sourceImage, Rectangle cropRect)
        {
            // Создаем новое изображение
            var croppedImage = new Bitmap(cropRect.Width, cropRect.Height);

            using (var graphics = Graphics.FromImage(croppedImage))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                // Рисуем обрезанную область
                graphics.DrawImage(
                    sourceImage,
                    new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                    cropRect,
                    GraphicsUnit.Pixel);
            }

            return croppedImage;
        }

        // Генерация имени файла
        private string GenerateFileName(FaceInfo faceInfo, string prefix)
        {
            string imageName = Path.GetFileNameWithoutExtension(faceInfo.ImagePath);

            // Добавляем координаты для уникальности
            string coords = $"{faceInfo.Location.Left}_{faceInfo.Location.Top}";

            return $"{prefix}_{imageName}_{coords}.png";
        }

        // Сохранение изображения с настройками качества
        private void SaveImage(DrawingImage image, string path)
        {
            image.Save(path);
        }
    }

}