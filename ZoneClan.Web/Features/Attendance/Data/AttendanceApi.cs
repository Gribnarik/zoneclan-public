using System.Net.Http.Json;
using ZoneClan.Core.Features.Attendance.ClanStats.Models;
using ZoneClan.Core.Features.Attendance.Excuses.Models;
using ZoneClan.Core.Features.Attendance.PlayerStats.Models;
using ZoneClan.Core.Features.Attendance.TabsList.Models;
using ZoneClan.Core.Features.Attendance.UploadTab.Models;
using ZoneClan.Web.Features.Shared;

namespace ZoneClan.Web.Features.Attendance.Data;

public class AttendanceApi(HttpClient http)
{
    public Task<WeekDeskDto> GetWeekAsync(DateOnly date) =>
        ReadAsync<WeekDeskDto>(http.GetAsync($"api/attendance/weeks/{date:yyyy-MM-dd}"));

    public async Task<AttendancePreviewDto> PreviewAsync(Stream file, string fileName, DateOnly weekStart, int stage)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file), "file", fileName);
        content.Add(new StringContent(weekStart.ToString("yyyy-MM-dd")), "weekStart");
        content.Add(new StringContent(stage.ToString()), "stage");
        return await ReadAsync<AttendancePreviewDto>(http.PostAsync("api/attendance/preview", content));
    }

        public async Task SaveAsync(AttendancePreviewDto draft)
    {
        var request = new SaveTabRequest
        {
            DraftId = draft.DraftId,
            WeekStart = draft.WeekStart,
            Stage = draft.Stage,
            Team1Name = draft.Team1Name,
            Team1Score = draft.Team1Score,
            Team2Name = draft.Team2Name,
            Team2Score = draft.Team2Score,
            Guilds = draft.Guilds, // <-- ДОБАВЛЕНО
            Players = draft.Players
        };
        using var response = await http.PostAsJsonAsync("api/attendance/tabs", request);
        await EnsureAsync(response);
    }

    public async Task UpdateTabAsync(int tabId, SaveTabRequest request)
    {
        using var response = await http.PutAsJsonAsync($"api/attendance/tabs/{tabId}", request);
        await EnsureAsync(response);
    }

    public async Task SetExcuseAsync(int tabId, int rowId, bool isExcused)
    {
        using var response = await http.PatchAsJsonAsync(
            $"api/attendance/tabs/{tabId}/players/{rowId}",
            new UpdatePlayerRowRequest { IsExcused = isExcused });
        await EnsureAsync(response);
    }

    public async Task DeleteTabAsync(int tabId)
    {
        using var response = await http.DeleteAsync($"api/attendance/tabs/{tabId}");
        await EnsureAsync(response);
    }

    public Task<PlayerCardDto> GetPlayerAsync(ulong? playerId, string? nickname, DateOnly from, DateOnly to)
    {
        var id = playerId is > 0 ? playerId.Value : 0;
        var nick = string.IsNullOrWhiteSpace(nickname) ? "" : Uri.EscapeDataString(nickname);
        return ReadAsync<PlayerCardDto>(http.GetAsync(
            $"api/attendance/players/{id}?nickname={nick}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));
    }

    public Task<List<PlayerListRowDto>> GetPlayerListAsync(DateOnly from, DateOnly to) =>
        ReadAsync<List<PlayerListRowDto>>(http.GetAsync($"api/attendance/players?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

    public async Task UpdateCharacteristicAsync(ulong playerId, int value)
    {
        using var response = await http.PutAsJsonAsync($"api/attendance/players/{playerId}/characteristic", value);
        await EnsureAsync(response);
    }

    public Task<ClanStatsDto> GetClanAsync(DateOnly from, DateOnly to) =>
        ReadAsync<ClanStatsDto>(http.GetAsync($"api/attendance/clan?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

    public Task<ExcuseMatrixDto> GetExcusesAsync(DateOnly from, DateOnly to) =>
        ReadAsync<ExcuseMatrixDto>(http.GetAsync($"api/attendance/excuses?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

    public async Task SetCellAsync(int tabId, ulong playerId, string status)
    {
        using var response = await http.PutAsJsonAsync(
            $"api/attendance/tabs/{tabId}/excuse",
            new SetExcuseRequest { PlayerId = playerId, Status = status });
        await ApiClient.EnsureAsync(response);
    }

    public async Task SetBulkAsync(int tabId, List<ulong> playerIds, string status)
    {
        using var response = await http.PutAsJsonAsync(
            $"api/attendance/tabs/{tabId}/excuse/bulk",
            new BulkExcuseRequest { TabId = tabId, PlayerIds = playerIds, Status = status });
        await ApiClient.EnsureAsync(response);
    }

    private static async Task<T> ReadAsync<T>(Task<HttpResponseMessage> request) =>
        await ApiClient.ReadAsync<T>(await request);

    private static Task EnsureAsync(HttpResponseMessage response) => ApiClient.EnsureAsync(response);
}
