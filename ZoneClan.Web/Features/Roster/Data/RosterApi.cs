using System.Net.Http.Json;
using ZoneClan.Core.Features.Roster.Models;
using ZoneClan.Web.Features.Shared;

namespace ZoneClan.Web.Features.Roster.Data;

public class RosterApi(HttpClient http)
{
    public async Task<List<RosterPlayerDto>> GetListAsync() =>
        await ApiClient.ReadAsync<List<RosterPlayerDto>>(await http.GetAsync("api/roster"));

    public async Task<RosterCardDto> GetAsync(ulong discordId) =>
        await ApiClient.ReadAsync<RosterCardDto>(await http.GetAsync($"api/roster/{discordId}"));

    public async Task<ulong> UpdateAsync(ulong discordId, UpdateRosterRequest request)
    {
        using var response = await http.PatchAsJsonAsync($"api/roster/{discordId}", request);
        await ApiClient.EnsureAsync(response);
        var saved = await response.Content.ReadFromJsonAsync<RosterSaveResult>();
        return saved?.DiscordId ?? request.DiscordId;
    }

    public async Task DeleteAsync(ulong discordId)
    {
        using var response = await http.DeleteAsync($"api/roster/{discordId}");
        await ApiClient.EnsureAsync(response);
    }
}
