using System.Net.Http.Json;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;
using Microsoft.Extensions.Logging;
using ZoneClan.Bot.Features.Settings.Data;
using ZoneClan.Core.Features.Penalties.Models;
using ZoneClan.Core.Features.Settings.Models;

namespace ZoneClan.Bot.Features.Reminders;

public class ReminderService(
    GatewayClient client,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<ReminderService> logger) : BackgroundService
{
    private readonly HashSet<string> _sent = new();
    private readonly HashSet<int> _pingedPenalties = new();
    private readonly HashSet<int> _pingedResolved = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LoadPinged();
        await SeedResolvedAsync();
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(); }
            catch (Exception ex) { logger.LogError(ex, "Reminder tick failed"); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private string PingedFile => Path.Combine(AppContext.BaseDirectory, "pinged_penalties.txt");

    private void LoadPinged()
    {
        try
        {
            if (File.Exists(PingedFile))
                foreach (var line in File.ReadAllLines(PingedFile))
                    if (int.TryParse(line, out var id)) _pingedPenalties.Add(id);
        }
        catch { }
    }

    private void SavePinged()
    {
        try { File.WriteAllLines(PingedFile, _pingedPenalties.Select(id => id.ToString())); }
        catch { }
    }

    private async Task SeedResolvedAsync()
    {
        try
        {
            using var http = httpClientFactory.CreateClient("api");
            var penalties = await http.GetFromJsonAsync<PenaltyPageDto>("api/penalties?resolved=true");
            foreach (var p in penalties?.Items ?? [])
                if (p.Status == "worked_off") _pingedResolved.Add(p.Id);
        }
        catch { }
    }

    private async Task TickAsync()
    {
        var now = DateTime.Now;
        var day = now.DayOfWeek;
        if (day is not (DayOfWeek.Thursday or DayOfWeek.Friday or DayOfWeek.Saturday)) 
        {
            await SendPenaltyPingsAsync();
            return;
        }

        var http = httpClientFactory.CreateClient("api");

        // gathering reminders between 19:00 and 20:00
        if (now.Hour == 19 && now.Minute >= 0 && now.Minute <= 59)
        {
            var gathering = new DateTime(now.Year, now.Month, now.Day, 19, 50, 0);
            var minutesLeft = (int)(gathering - now).TotalMinutes;
            if (minutesLeft < 0) minutesLeft = 0;

            if (now.Minute == 0 || now.Minute == 50 || now.Minute == 0)
            {
                // 19:00 ping (50 min left), 19:50 ping (gathering now)
                if (now.Minute == 0)
                    await SendGatheringPingAsync(http, $"⏰ До сбора {minutesLeft} минут (сбор в 19:50).", now, "gather50");
                if (now.Minute == 50)
                    await SendGatheringPingAsync(http, "⏰ Сбор сейчас! Все к 19:50.", now, "gather0");
            }
        }
        else if (now.Hour == 20 && now.Minute == 0)
        {
            await SendAbsentPingAsync(http, now);
        }

        await SendPenaltyPingsAsync();
    }

    private async Task SendGatheringPingAsync(HttpClient http, string text, DateTime now, string key)
    {
        var dateKey = $"{now:yyyy-MM-dd}-{key}";
        if (_sent.Contains(dateKey)) return;

        // get guilds from settings? We only know one guild from appsettings. Use configured guild.
        var guildId = 1534644851663175701UL;
        var settings = await SettingsApi.GetAsync(http, guildId);
        var channel = settings.GatheringChannelId ?? 0;
        if (channel == 0) return;

        await client.Rest.SendMessageAsync(channel, new MessageProperties { Content = text });
        _sent.Add(dateKey);
    }

    private async Task SendAbsentPingAsync(HttpClient http, DateTime now)
    {
        var dateKey = $"{now:yyyy-MM-dd}-absent";
        if (_sent.Contains(dateKey)) return;

        var guildId = 1534644851663175701UL;
        var settings = await SettingsApi.GetAsync(http, guildId);
        var channel = settings.AbsentChannelId ?? 0;
        if (channel == 0) return;

        // get roster (players) - ping all? Better: players who didn't show. For now ping @here
        await client.Rest.SendMessageAsync(channel, new MessageProperties { Content = "⏰ 20:00 — сбор начался. Кто не пришёл и не отписался — отметьтесь." });
        _sent.Add(dateKey);
    }

    private async Task SendPenaltyPingsAsync()
    {
        var http = httpClientFactory.CreateClient("api");
        var guildId = 1534644851663175701UL;
        var settings = await SettingsApi.GetAsync(http, guildId);
        var channel = settings.PenaltyChannelId ?? 0;
        if (channel == 0) return;

        try
        {
            var penalties = await http.GetFromJsonAsync<PenaltyPageDto>("api/penalties?resolved=true");
            var items = penalties?.Items ?? [];
            foreach (var p in items)
            {
                if (p.Status == "active")
                {
                    if (_pingedPenalties.Contains(p.Id)) continue;
                    _pingedPenalties.Add(p.Id);
                    SavePinged();
                    await client.Rest.SendMessageAsync(channel, new MessageProperties
                    {
                        Content = $"⚠️ Новый штраф: <@{p.PlayerId}> — {p.TypeName} ({p.Points} очк.) {p.Reason}"
                    });
                }
                else if (p.Status == "worked_off")
                {
                    if (_pingedResolved.Contains(p.Id)) continue;
                    _pingedResolved.Add(p.Id);
                    await client.Rest.SendMessageAsync(channel, new MessageProperties
                    {
                        Content = $"✅ Штраф погашен: <@{p.PlayerId}> — {p.TypeName} ({p.ResolutionMethod ?? "погашен"})"
                    });
                }
            }
        }
        catch (Exception ex) { logger.LogError(ex, "Penalty ping failed"); }
    }

    private record PenaltyPageDto(List<PenaltyTypeDto> Types, List<PenaltyDto> Items, List<PenaltySummaryDto> Summaries);
}
