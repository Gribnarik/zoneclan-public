using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FuzzySharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ZoneClan.Server.Features.Attendance.UploadTab.BusinessLogic;

public class OcrPlayerResult
{
    public string Nickname { get; set; } = "";
    public bool Known { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Score { get; set; }
}

public class MatchInfo
{
    public string Team1Name { get; set; } = "";
    public int Team1Score { get; set; }
    public string Team2Name { get; set; } = "";
    public int Team2Score { get; set; }
}

public class OcrService(IWebHostEnvironment env)
{
    private const string Whitelist =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_" +
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";

    private const int BaseWidth = 1920;
    private const int BaseHeight = 1080;

    private readonly Dictionary<string, (float x, float y, float w, float h)> _relativeRects = new()
    {
        ["Nick"] = (691f / BaseWidth, 210f / BaseHeight, 396f / BaseWidth, 735f / BaseHeight),
        ["Kills"] = (1196f / BaseWidth, 210f / BaseHeight, 70f / BaseWidth, 735f / BaseHeight),
        ["Deaths"] = (1289f / BaseWidth, 210f / BaseHeight, 70f / BaseWidth, 735f / BaseHeight),
        ["Assists"] = (1375f / BaseWidth, 210f / BaseHeight, 70f / BaseWidth, 735f / BaseHeight),
        ["Score"] = (1565f / BaseWidth, 210f / BaseHeight, 80f / BaseWidth, 735f / BaseHeight),
        ["Team1Name"] = (150f / BaseWidth, 259f / BaseHeight, 367f / BaseWidth, 24f / BaseHeight),
        ["Team2Name"] = (150f / BaseWidth, 294f / BaseHeight, 367f / BaseWidth, 24f / BaseHeight),
        ["Team1Score"] = (517f / BaseWidth, 259f / BaseHeight, 49f / BaseWidth, 24f / BaseHeight),
        ["Team2Score"] = (517f / BaseWidth, 294f / BaseHeight, 49f / BaseWidth, 24f / BaseHeight)
    };

    public async Task<List<OcrPlayerResult>> ProcessTabAsync(string fullPath, IReadOnlyList<string> knownNicks)
    {
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Изображение не найдено", fullPath);

        using var src = await Image.LoadAsync<Rgba32>(fullPath);
        var width = src.Width;
        var height = src.Height;

        // 1. Определяем координаты колонок (адаптивно)
        var nickX = (int)(691f / BaseWidth * width);
        var nickW = (int)(396f / BaseWidth * width);
        var killsX = (int)(1196f / BaseWidth * width);
        var deathsX = (int)(1289f / BaseWidth * width);
        var assistsX = (int)(1375f / BaseWidth * width);
        var scoreX = (int)(1565f / BaseWidth * width);

        var tableRight = scoreX + (int)(80f / BaseWidth * width);

        // 2. Определяем границы таблицы автоматически
        var (topY, bottomY) = DetectTableBounds(src, nickX, tableRight);

        // 3. Детектируем отдельные строки
        var rowRects = DetectPlayerRows(src, nickX, tableRight, topY, bottomY);

        // 4. Обрабатываем каждую строку
        var results = new List<OcrPlayerResult>();

        foreach (var rowRect in rowRects)
        {
            try
            {
                // OCR для никнейма (PSM 7 = single line)
                var nickRect = new Rectangle(nickX, rowRect.Y, nickW, rowRect.Height);
                var nickBytes = PreprocessForOcr(src, nickRect);
                var nickText = await RunTesseractAsync(nickBytes, "rus+eng", 7, Whitelist);

                var rawNick = CleanNickname(nickText.Trim());
                if (rawNick.Length < 3) continue;

                // OCR для чисел (kills, deaths, assists, score)
                var numbersRect = new Rectangle(killsX, rowRect.Y, tableRight - killsX, rowRect.Height);
                var numbersBytes = PreprocessForOcr(src, numbersRect);
                var numbersText = await RunTesseractAsync(numbersBytes, "eng", 7, "0123456789");

                // Парсинг чисел
                var numbers = Regex.Matches(numbersText, @"\d+")
                    .Select(m => int.Parse(m.Value))
                    .ToList();

                if (numbers.Count < 4) continue; // Должно быть минимум 4 числа

                var kills = numbers[0];
                var deaths = numbers[1];
                var assists = numbers[2];
                var score = numbers[^1]; // Последнее число = score

                var (nickname, known) = MatchNick(rawNick, knownNicks);

                results.Add(new OcrPlayerResult
                {
                    Nickname = nickname,
                    Known = known,
                    Kills = kills,
                    Deaths = deaths,
                    Assists = assists,
                    Score = score
                });
            }
            catch (Exception)
            {
                // Пропускаем проблемные строки, продолжаем обработку
                continue;
            }
        }

        return results;
    }

    public async Task<MatchInfo> ProcessMatchInfoAsync(string fullPath)
    {
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Изображение не найдено", fullPath);

        using var src = await Image.LoadAsync<Rgba32>(fullPath);
        var team1Name = CleanClanName((await ExtractTextAsync(src, GetAdaptiveRect("Team1Name", src.Width, src.Height))).FirstOrDefault() ?? "");
        var team1Score = ParseInt((await ExtractTextAsync(src, GetAdaptiveRect("Team1Score", src.Width, src.Height))).FirstOrDefault() ?? "0");
        var team2Name = CleanClanName((await ExtractTextAsync(src, GetAdaptiveRect("Team2Name", src.Width, src.Height))).FirstOrDefault() ?? "");
        var team2Score = ParseInt((await ExtractTextAsync(src, GetAdaptiveRect("Team2Score", src.Width, src.Height))).FirstOrDefault() ?? "0");

        return new MatchInfo
        {
            Team1Name = team1Name,
            Team1Score = team1Score,
            Team2Name = team2Name,
            Team2Score = team2Score
        };
    }

    private Rectangle GetAdaptiveRect(string name, int imageWidth, int imageHeight)
    {
        if (!_relativeRects.TryGetValue(name, out var rel))
            throw new ArgumentException($"Unknown rect: {name}");

        return Clamp(new Rectangle(
            (int)(rel.x * imageWidth),
            (int)(rel.y * imageHeight),
            Math.Max(1, (int)(rel.w * imageWidth)),
            Math.Max(1, (int)(rel.h * imageHeight))), imageWidth, imageHeight);
    }

    private static Rectangle Clamp(Rectangle rect, int width, int height)
    {
        var x = Math.Clamp(rect.X, 0, Math.Max(0, width - 1));
        var y = Math.Clamp(rect.Y, 0, Math.Max(0, height - 1));
        var w = Math.Clamp(rect.Width, 1, width - x);
        var h = Math.Clamp(rect.Height, 1, height - y);
        return new Rectangle(x, y, w, h);
    }

    private async Task<List<string>> ExtractTextAsync(Image<Rgba32> src, Rectangle rect)
    {
        var text = await RunTesseractAsync(PreprocessForOcr(src, rect), "rus+eng", 7, Whitelist);
        return text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    private static async Task<string> RunTesseractAsync(byte[] imageBytes, string lang, int psm, string whitelist)
    {
        var tmpIn = Path.Combine(Path.GetTempPath(), $"ocr_in_{Guid.NewGuid():N}.png");
        var tmpOut = Path.Combine(Path.GetTempPath(), $"ocr_out_{Guid.NewGuid():N}");
        var tmpTxt = tmpOut + ".txt";
        await File.WriteAllBytesAsync(tmpIn, imageBytes);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{tmpIn}\" \"{tmpOut}\" -l {lang} --oem 3 --psm {psm} -c tessedit_char_whitelist={whitelist}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = System.Diagnostics.Process.Start(psi)
                ?? throw new InvalidOperationException("Не удалось запустить tesseract.");
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();
            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"tesseract exit={proc.ExitCode}, stderr: {stderr}");
            if (!File.Exists(tmpTxt))
                throw new FileNotFoundException($"tesseract не создал {tmpTxt}");
            return await File.ReadAllTextAsync(tmpTxt, Encoding.UTF8);
        }
        finally
        {
            if (File.Exists(tmpIn)) File.Delete(tmpIn);
            if (File.Exists(tmpTxt)) File.Delete(tmpTxt);
        }
    }

    private static byte[] PreprocessForOcr(Image<Rgba32> src, Rectangle rect)
    {
        using var original = src.Clone(ctx =>
            ctx.Crop(rect)
               .Resize(rect.Width * 4, rect.Height * 4, KnownResamplers.Lanczos3)
               .Grayscale());
        using var gray = original.CloneAs<L8>();
        using var blurred = gray.Clone(ctx => ctx.GaussianBlur(2f));

        using var result = new Image<L8>(gray.Width, gray.Height);
        for (var y = 0; y < gray.Height; y++)
        {
            for (var x = 0; x < gray.Width; x++)
            {
                var val = gray[x, y].PackedValue * 1.6f - blurred[x, y].PackedValue * 0.6f;
                result[x, y] = new L8((byte)Math.Clamp(val, 0f, 255f));
            }
        }

        result.Mutate(ctx =>
        {
            ctx.BinaryThreshold(210f / 255f);
            ctx.Invert();
        });

        using var ms = new MemoryStream();
        result.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static (string Nickname, bool Known) MatchNick(string nickname, IReadOnlyList<string> knownNicks)
    {
        var exact = knownNicks.FirstOrDefault(nick => string.Equals(nick, nickname, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return (exact, true);
        if (knownNicks.Count == 0)
            return (nickname, false);

        var fuzzy = FuzzySharp.Process.ExtractOne(nickname, knownNicks, cutoff: 60);
        return fuzzy is null ? (nickname, false) : (fuzzy.Value, true);
    }

    private static string CleanNickname(string raw) =>
        Regex.Replace(raw, @"[^a-zA-Z0-9_а-яА-ЯёЁ]", "");

    private static string CleanClanName(string raw) =>
        Regex.Replace(Regex.Replace(raw, @"[^a-zA-Zа-яА-ЯёЁ0-9\s]", "").Trim(), @"\s+", " ");

    private static int ParseInt(string raw) =>
        int.TryParse(Regex.Replace(raw, @"[^0-9]", ""), out var value) ? value : 0;

    private (int TopY, int BottomY) DetectTableBounds(Image<Rgba32> src, int leftX, int rightX)
    {
        // 1. Конвертируем в grayscale регион никнейма
        var sampleWidth = 200;
        var scanRect = new Rectangle(leftX, 150, sampleWidth, src.Height - 200);

        using var sample = src.Clone(ctx => ctx.Crop(scanRect).Grayscale());
        using var gray = sample.CloneAs<L8>();

        // 2. Считаем горизонтальную проекцию (сумма яркости по каждой строке)
        var projection = new int[gray.Height];
        for (var y = 0; y < gray.Height; y++)
        {
            int sum = 0;
            for (var x = 0; x < gray.Width; x++)
                sum += 255 - gray[x, y].PackedValue; // Инвертируем (текст = белый)
            projection[y] = sum;
        }

        // 3. Находим первую и последнюю строку с текстом
        var threshold = sampleWidth * 10; // Минимальная яркость для строки с текстом
        var topY = -1;
        var bottomY = -1;

        for (var y = 0; y < projection.Length; y++)
        {
            if (projection[y] > threshold)
            {
                if (topY == -1) topY = y;
                bottomY = y;
            }
        }

        // 4. Добавляем отступы и возвращаем абсолютные координаты
        return (
            TopY: Math.Max(0, topY + 150 - 10), // +150 offset scanRect, -10 padding
            BottomY: Math.Min(src.Height, bottomY + 150 + 10)
        );
    }

    private List<Rectangle> DetectPlayerRows(Image<Rgba32> src, int leftX, int rightX, int topY, int bottomY)
    {
        var width = rightX - leftX;
        var height = bottomY - topY;

        // Предполагаем примерную высоту строки из базовых координат
        var expectedRowHeight = (int)(735f / 30f * src.Height / BaseHeight); // ~25-30px для 1080p
        var minRowHeight = expectedRowHeight / 2;

        // Простой подход: делим регион на равные части по ожидаемой высоте строки
        var rows = new List<Rectangle>();
        var currentY = topY;
        var rowIndex = 0;

        while (currentY + minRowHeight < bottomY && rowIndex < 35)
        {
            var rowHeight = Math.Min(expectedRowHeight, bottomY - currentY);
            if (rowHeight < minRowHeight) break;

            rows.Add(new Rectangle(leftX, currentY, width, rowHeight));
            currentY += rowHeight;
            rowIndex++;
        }

        return rows;
    }

    public string UploadsRoot()
    {
        var root = Path.Combine(env.WebRootPath, "uploads", "tabs");
        Directory.CreateDirectory(root);
        return root;
    }
}
