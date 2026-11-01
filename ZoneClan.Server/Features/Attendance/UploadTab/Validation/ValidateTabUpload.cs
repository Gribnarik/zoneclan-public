using ZoneClan.Core.Features.Attendance.Shared;

namespace ZoneClan.Server.Features.Attendance.UploadTab.Validation;

public static class ValidateTabUpload
{
    public const long MaxFileSize = 5 * 1024 * 1024;

    public static string? Date(DateOnly weekStart, int stage)
    {
        if (weekStart == default)
            return "Укажите дату начала недели";
        if (stage is < 1 or > 3)
            return "Этап должен быть от 1 до 3";
        if (!ClanMatch.IsTabDay(weekStart))
            return $"Таб можно загрузить только в четверг, пятницу или субботу. Выбрано: {weekStart:dd.MM.yyyy}";
        return null;
    }

    public static string? File(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "Выберите файл";
        if (file.Length > MaxFileSize)
            return "Файл больше 5 МБ";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".png" and not ".jpg" and not ".jpeg")
            return "Нужен скрин PNG или JPEG";
        return null;
    }
}
