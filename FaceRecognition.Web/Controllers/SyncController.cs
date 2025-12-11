using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _uploadsPath;

    public SyncController(IWebHostEnvironment environment)
    {
        _environment = environment;
        _uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads");

        // Создаем папку если её нет
        if (!Directory.Exists(_uploadsPath))
            Directory.CreateDirectory(_uploadsPath);
    }

    // Очистка папки Uploads
    [HttpPost("clear")]
    public IActionResult ClearUploads()
    {
        try
        {
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
                return Ok(new { message = "Папка создана" });
            }

            // Удаляем все файлы в папке
            var files = Directory.GetFiles(_uploadsPath);
            foreach (var file in files)
            {
                System.IO.File.Delete(file);
            }

            // Удаляем все подпапки
            var directories = Directory.GetDirectories(_uploadsPath);
            foreach (var dir in directories)
            {
                Directory.Delete(dir, true);
            }

            return Ok(new
            {
                clearedFiles = files.Length,
                clearedDirs = directories.Length,
                message = "Папка очищена"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Загрузка файла (Data URL или обычный файл)
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] bool isDataUrl = false)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл пустой");

            // Безопасное имя файла
            var safeFileName = GetSafeFileName(file.FileName);
            var filePath = Path.Combine(_uploadsPath, safeFileName);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new
            {
                fileName = safeFileName,
                filePath = filePath,
                size = file.Length
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Загрузка фото по URL
    [HttpPost("uploadUrl")]
    public async Task<IActionResult> UploadFromUrl([FromBody] UrlUploadRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Url))
                return BadRequest("URL не указан");

            using var httpClient = new HttpClient();

            // Скачиваем файл
            var bytes = await httpClient.GetByteArrayAsync(request.Url);

            // Определяем имя файла
            var fileName = !string.IsNullOrEmpty(request.FileName)
                ? GetSafeFileName(request.FileName)
                : $"url_photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

            var filePath = Path.Combine(_uploadsPath, fileName);

            // Сохраняем
            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            return Ok(new
            {
                fileName = fileName,
                filePath = filePath,
                size = bytes.Length
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Получение списка файлов в папке (для проверки)
    [HttpGet("files")]
    public IActionResult GetFiles()
    {
        try
        {
            if (!Directory.Exists(_uploadsPath))
                return Ok(new List<object>());

            var files = Directory.GetFiles(_uploadsPath)
                .Select(f => new
                {
                    Name = Path.GetFileName(f),
                    Path = f,
                    Size = new FileInfo(f).Length,
                    Modified = System.IO.File.GetLastWriteTime(f)
                })
                .ToList();

            return Ok(files);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Вспомогательный метод для безопасного имени файла
    private string GetSafeFileName(string fileName)
    {
        // Убираем небезопасные символы
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(fileName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray());

        // Если имя стало пустым, генерируем новое
        if (string.IsNullOrEmpty(safeName))
        {
            safeName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        }

        // Добавляем timestamp если файл уже существует
        var path = Path.Combine(_uploadsPath, safeName);
        if (System.IO.File.Exists(path))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(safeName);
            var ext = Path.GetExtension(safeName);
            var timestamp = DateTime.Now.ToString("HHmmss");
            safeName = $"{nameWithoutExt}_{timestamp}{ext}";
        }

        return safeName;
    }

    // Модель для запроса
    public class UrlUploadRequest
    {
        public string Url { get; set; }
        public string FileName { get; set; }
    }
}