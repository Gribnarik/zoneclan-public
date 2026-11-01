using ZoneClan.Core.Features.Attendance.Shared;
using ZoneClan.Core.Features.Attendance.UploadTab.Models;
using ZoneClan.Server.Features.Attendance.UploadTab.Data;
using ZoneClan.Server.Features.Attendance.UploadTab.Validation;

namespace ZoneClan.Server.Features.Attendance.UploadTab.BusinessLogic;

public class PreviewTabHandler(OcrService ocr, AttendanceDraftStore drafts, AttendanceReadStore store)
{
    public async Task<AttendancePreviewDto> ExecuteAsync(IFormFile file, DateOnly weekStart, int stage)
    {
        var dateError = ValidateTabUpload.Date(weekStart, stage);
        if (dateError is not null)
            throw new InvalidOperationException(dateError);
        var fileError = ValidateTabUpload.File(file);
        if (fileError is not null)
            throw new InvalidOperationException(fileError);
        if (await store.TabExistsAsync(weekStart, stage))
            throw new InvalidOperationException("Таб на эту дату и этап уже есть");

        var id = Guid.NewGuid().ToString("N");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{id}{ext}";
        var fullPath = Path.Combine(ocr.UploadsRoot(), fileName);
        await using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream);

        try
        {
            var known = await store.GetPlayersAsync();
            var match = await ocr.ProcessMatchInfoAsync(fullPath);
            var rows = await ocr.ProcessTabAsync(fullPath, known.Select(player => player.Nickname).Distinct().ToList());
            var byNick = known
                .GroupBy(player => player.Nickname, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().DiscordId, StringComparer.OrdinalIgnoreCase);

            var (recognized, isWinner) = ClanMatch.Evaluate(match.Team1Name, match.Team1Score, match.Team2Name, match.Team2Score);
            drafts.Put(new AttendanceDraft
            {
                Id = id,
                RelativePath = $"/uploads/tabs/{fileName}",
                WeekStart = weekStart,
                Stage = stage
            });

            var message = recognized
                ? $"{(isWinner ? "Победа" : "Поражение")}. Счёт {match.Team1Score}:{match.Team2Score}. Игроков: {rows.Count}. Проверьте строки и сохраните."
                : $"Клан {ClanMatch.ClanName} не распознан в шапке ({match.Team1Name} / {match.Team2Name}). Поправьте названия и сохраните — таб не пойдёт в сводку, пока клан не указан.";

            return new AttendancePreviewDto
            {
                DraftId = id,
                WeekStart = weekStart,
                Stage = stage,
                Team1Name = match.Team1Name,
                Team1Score = match.Team1Score,
                Team2Name = match.Team2Name,
                Team2Score = match.Team2Score,
                ClanRecognized = recognized,
                IsWinner = isWinner,
                Message = message,
                KnownPlayers = known.Select(player => new KnownNickDto
                {
                    DiscordId = player.DiscordId,
                    Nickname = player.Nickname
                }).OrderBy(player => player.Nickname).ToList(),
                Players = rows
                    .OrderByDescending(row => row.Score)
                    .Select(row => new AttendancePlayerDraft
                {
                    Nickname = row.Nickname,
                    PlayerId = byNick.TryGetValue(row.Nickname, out var playerId) ? playerId : null,
                    Known = row.Known && byNick.ContainsKey(row.Nickname),
                    Kills = row.Kills,
                    Deaths = row.Deaths,
                    Assists = row.Assists,
                    Score = row.Score
                }).ToList()
            };
        }
        catch
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            throw;
        }
    }
}
