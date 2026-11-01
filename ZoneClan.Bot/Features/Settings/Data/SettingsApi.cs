using System.Net.Http.Json;
using ZoneClan.Core.Features.Settings.Models;

namespace ZoneClan.Bot.Features.Settings.Data;

internal static class SettingsApi
{
    public static async Task<ClanSettingsDto> GetAsync(HttpClient client, ulong guildId) =>
        await client.GetFromJsonAsync<ClanSettingsDto>($"api/guilds/{guildId}/settings")
        ?? new ClanSettingsDto { GuildId = guildId };
}
