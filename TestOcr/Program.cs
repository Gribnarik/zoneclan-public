using ZoneClan.Server.Features.Attendance.UploadTab.BusinessLogic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

var tabsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Документы/проект/Табы");
var imageFiles = Directory.GetFiles(tabsFolder, "*.png");

Console.WriteLine($"Найдено {imageFiles.Length} скриншотов для тестирования\n");

var env = new MockWebHostEnvironment();
var service = new OcrService(env);

var knownNicks = new List<string>
{
    "Gribnarik", "Ykpyfn", "Kolonich", "Palapenda",
    "ФААУСТ", "Ulti_Dem", "Ya_KOSTR", "Pendif", "chubakas"
};

foreach (var imagePath in imageFiles)
{
    Console.WriteLine($"=== Обработка: {Path.GetFileName(imagePath)} ===");
    try
    {
        // Загружаем изображение для проверки размера
        using var img = await SixLabors.ImageSharp.Image.LoadAsync(imagePath);
        Console.WriteLine($"Размер изображения: {img.Width}x{img.Height}");

        var results = await service.ProcessTabAsync(imagePath, knownNicks);

        Console.WriteLine($"Распознано игроков: {results.Count}");
        if (results.Count > 0)
        {
            Console.WriteLine("\nТоп 5 игроков:");
            foreach (var player in results.Take(5))
            {
                var knownMark = player.Known ? "✓" : "?";
                Console.WriteLine($"  {knownMark} {player.Nickname,-20} K:{player.Kills,2} D:{player.Deaths,2} A:{player.Assists,2} Score:{player.Score}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Не распознан ни один игрок!");
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ОШИБКА: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}\n");
    }
}

Console.WriteLine("Тестирование завершено!");

public class MockWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "/tmp";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; set; } = "TestOcr";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = "/tmp";
    public string EnvironmentName { get; set; } = "Development";
}
