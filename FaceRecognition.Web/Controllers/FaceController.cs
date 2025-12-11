using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class FaceController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FaceController> _logger;

    public FaceController(IWebHostEnvironment environment, ILogger<FaceController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    // Запуск FaceApplication
    [HttpPost("run")]
    public IActionResult RunFaceRecognition()
    {
        try
        {
            // Пути к папкам
            var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads");
            var visualizedPath = Path.Combine(_environment.ContentRootPath, "Visualized");

            // Проверяем существование папок
            if (!Directory.Exists(uploadsPath))
            {
                return BadRequest(new { success = false, message = "Папка Uploads не существует" });
            }

            // Проверяем, есть ли файлы в Uploads
            var files = Directory.GetFiles(uploadsPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

            if (!files.Any())
            {
                return BadRequest(new { success = false, message = "В папке Uploads нет фотографий" });
            }

            if (Directory.Exists(visualizedPath))
            {
                Directory.Delete(visualizedPath, true);
            }
            Directory.CreateDirectory(visualizedPath);

            FaceRecognitionApp.FaceApplication.Run(uploadsPath, visualizedPath);

            var result = GetVisualizationResult(visualizedPath);

            return Ok(new
            {
                success = true,
                message = "Обработка завершена",
                personsCount = result.Count,
                photosCount = result.Sum(p => p.PhotosCount)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске FaceApplication");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // Проверка папки Uploads
    [HttpGet("check-uploads")]
    public IActionResult CheckUploads()
    {
        var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads");

        if (!Directory.Exists(uploadsPath))
        {
            return Ok(new { hasFiles = false, message = "Папка не существует" });
        }

        var files = Directory.GetFiles(uploadsPath);
        return Ok(new
        {
            hasFiles = files.Length > 0,
            fileCount = files.Length
        });
    }

    // Получение данных для визуализации
    [HttpGet("visualization")]
    public IActionResult GetVisualization()
    {
        try
        {
            var visualizedPath = Path.Combine(_environment.ContentRootPath, "Visualized");

            if (!Directory.Exists(visualizedPath))
            {
                return Ok(new List<object>());
            }

            var result = GetVisualizationResult(visualizedPath);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Вспомогательные методы
    private void ClearDirectory(string path)
    {
        foreach (var file in Directory.GetFiles(path))
        {
            System.IO.File.Delete(file);
        }

        foreach (var dir in Directory.GetDirectories(path))
        {
            Directory.Delete(dir, true);
        }
    }

    // Метод для получения результатов визуализации
    private List<PersonGroup> GetVisualizationResult(string visualizedPath)
    {
        var result = new List<PersonGroup>();

        if (!Directory.Exists(visualizedPath))
            return result;

        // Ищем папки Person_0, Person_1 и т.д.
        var personFolders = Directory.GetDirectories(visualizedPath)
            .Where(d => Path.GetFileName(d).StartsWith("Person "))
            .OrderBy(d => d);

        foreach (var folder in personFolders)
        {
            var folderName = Path.GetFileName(folder);
            var photos = Directory.GetFiles(folder, "*.*")
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .Select(f => new PhotoInfo 
                {
                    Name = Path.GetFileName(f),
                    Url = $"/Visualized/{folderName}/{Path.GetFileName(f)}"
                })
                .ToList();

            if (photos.Any())
            {
                result.Add(new PersonGroup
                {
                    FolderName = folderName,
                    Photos = photos, 
                    PhotosCount = photos.Count
                });
            }
        }

        return result;
    }

    // Модели
    public class PhotoInfo
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class PersonGroup
    {
        public string FolderName { get; set; }
        public List<PhotoInfo> Photos { get; set; } 
        public int PhotosCount { get; set; }
    }
}