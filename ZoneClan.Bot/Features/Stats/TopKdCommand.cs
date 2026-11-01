using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using ZoneClan.Core.Features.Roster.Models;

namespace ZoneClan.Bot.Features.Stats;

public class TopKdCommands(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("топ_кд", "Топ игроков по K/D (по убыванию)")]
    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    public async Task TopKdAsync()
    {
        using var client = httpClientFactory.CreateClient("api");
        List<RosterPlayerDto> players;
        try
        {
            players = await client.GetFromJsonAsync<List<RosterPlayerDto>>("api/roster") ?? [];
        }
        catch
        {
            await RespondAsync(DiscordUi.PrivateText("❌ Не удалось загрузить состав."));
            return;
        }

        var sorted = players.OrderByDescending(p => p.Kd).Take(15).ToList();
        var lines = sorted.Select((p, i) => $"{i + 1}. {p.Nickname} — {p.Kd:0.##} ({p.Kills}/{p.Deaths})").ToList();
        var text = "🏆 **Топ по K/D**\n" + string.Join("\n", lines);

        await RespondAsync(DiscordUi.PrivateText(text));
    }
}
