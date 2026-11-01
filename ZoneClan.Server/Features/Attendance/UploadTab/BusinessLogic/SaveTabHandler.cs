using ZoneClan.Core.Features.Attendance.Shared;
using ZoneClan.Core.Features.Attendance.UploadTab.Models;
using ZoneClan.Server.Features.Attendance.UploadTab.Data;
using ZoneClan.Server.Features.Attendance.UploadTab.Validation;

namespace ZoneClan.Server.Features.Attendance.UploadTab.BusinessLogic;

public class SaveTabHandler(AttendanceDraftStore drafts, AttendanceReadStore players, SaveTabCommand command)
{
    public async Task<int> ExecuteAsync(SaveTabRequest request)
    {
        var dateError = ValidateTabUpload.Date(request.WeekStart, request.Stage);
        if (dateError is not null)
            throw new InvalidOperationException(dateError);
        if (await players.TabExistsAsync(request.WeekStart, request.Stage))
            throw new InvalidOperationException("Таб на эту дату и этап уже есть");

        var draft = drafts.Take(request.DraftId);
        if (draft is null || draft.WeekStart != request.WeekStart || draft.Stage != request.Stage)
            throw new InvalidOperationException("Черновик не найден. Распознайте скрин ещё раз.");

        var known = await players.GetPlayersAsync();
        var byNick = known
            .GroupBy(player => player.Nickname, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().DiscordId, StringComparer.OrdinalIgnoreCase);

        foreach (var player in request.Players)
        {
            player.Nickname = player.Nickname.Trim();
            if (player.Nickname.Length == 0)
                throw new InvalidOperationException("У строки пустой ник");
            if (player.PlayerId is > 0)
            {
                var named = known.FirstOrDefault(item => item.DiscordId == player.PlayerId);
                if (named is not null && string.IsNullOrWhiteSpace(player.Nickname))
                    player.Nickname = named.Nickname;
            }
            else if (byNick.TryGetValue(player.Nickname, out var id))
            {
                player.PlayerId = id;
            }
            else
            {
                player.PlayerId = NickMatch.FindPlayerId(
                    player.Nickname,
                    known.Select(item => (item.DiscordId, item.Nickname)).ToList());
            }

            player.Known = player.PlayerId is not null;
        }

        var (recognized, isWinner) = ClanMatch.Evaluate(
            request.Team1Name, request.Team1Score, request.Team2Name, request.Team2Score);
        try
        {
            return await command.ExecuteAsync(request, draft.RelativePath, recognized, isWinner);
        }
        catch
        {
            drafts.Put(draft);
            throw;
        }
    }
}
