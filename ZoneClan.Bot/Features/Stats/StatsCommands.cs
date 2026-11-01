using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using ZoneClan.Core.Features.Roster.Models;
using ZoneClan.Core.Features.Squads.Models;

namespace ZoneClan.Bot.Features.Stats;

public class StatsCommands(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("статистика", "Показать карточку и статистику игрока")]
    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    public async Task StatsAsync(User? target = null)
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var guild = Context.Guild!;
        var user = target ?? Context.User;
        var isSelf = user.Id == Context.User.Id;

        using var client = httpClientFactory.CreateClient("api");
        var settings = await SettingsApi.GetAsync(client, guild.Id);

        var allowed = isSelf
            || GuildAccess.CanManageGuild(guild, Context.User)
            || (settings.TimekeeperRoleId is > 0 && GuildAccess.HasStaffRole(guild, Context.User, settings.TimekeeperRoleId))
            || (settings.CallerRoleId is > 0 && GuildAccess.HasStaffRole(guild, Context.User, settings.CallerRoleId))
            || (settings.DuelistRoleId is > 0 && GuildAccess.HasStaffRole(guild, Context.User, settings.DuelistRoleId));

        if (!allowed)
        {
            await ModifyResponseAsync(o => { o.Flags = MessageFlags.Ephemeral; o.Content = "❌ Чужую статистику могут смотреть только табельщики, коллеры, дуэлянты и руководство."; });
            return;
        }

        RosterCardDto? card;
        try { card = await client.GetFromJsonAsync<RosterCardDto>($"api/roster/{user.Id}"); }
        catch { card = null; }

        string squadInfo = "нет";
        try
        {
            var squads = await client.GetFromJsonAsync<List<SquadDto>>("api/squads");
            if (squads is not null)
            {
                foreach (var sq in squads)
                {
                    var m = sq.Members.FirstOrDefault(m => m.PlayerId == user.Id);
                    if (m is not null)
                    {
                        var role = m.Role switch { "commander" => "командир", "deputy" => "зам", _ => "боец" };
                        squadInfo = $"{sq.Name} — {role}" + (string.IsNullOrWhiteSpace(m.ExtraRole) ? "" : $" ({m.ExtraRole})");
                        break;
                    }
                }
            }
        }
        catch { }

        if (card is null)
        {
            await ModifyResponseAsync(o => { o.Flags = MessageFlags.Ephemeral; o.Content = "❌ Карточка игрока не найдена."; });
            return;
        }

        var lines = card.Lines ?? [];
        var kills = lines.Sum(l => l.Kills);
        var deaths = lines.Sum(l => l.Deaths);
        var assists = lines.Sum(l => l.Assists);
        var score = lines.Sum(l => l.Score);
        var kd = deaths == 0 ? kills : Math.Round((double)kills / deaths, 2);
        var kda = deaths == 0 ? kills + assists : Math.Round((double)(kills + assists) / deaths, 2);
        var avgScore = lines.Count > 0 ? Math.Round((double)score / lines.Count, 0) : 0;
        var roles = card.Roles.Count == 0 ? "нет" : string.Join(", ", card.Roles.Select(r => r.Name));
        var snipers = card.Snipers.Where(s => s.Selected).Select(s => s.Name).ToList();
        var snipersText = snipers.Count == 0 ? "нет" : string.Join(", ", snipers);

        var favorite = card.Armors.FirstOrDefault(a => a.IsFavorite);
        var others = card.Armors.Where(a => !a.IsFavorite).ToList();

        var fields = new List<EmbedFieldProperties>();

        fields.Add(new EmbedFieldProperties
        {
            Name = "Статус / Отряд",
            Value = $"{StatusName(card.Status)} | Отряд: {squadInfo}\nКувалда: {(card.HasSledgehammer ? "да" : "нет")} · Снайпер: {(card.PlaysSniper ? "да" : "нет")}" + (card.PlaysSniper && snipers.Count > 0 ? $"\nВинтовки: {snipersText}" : ""),
            Inline = false
        });

        fields.Add(new EmbedFieldProperties
        {
            Name = "Статистика",
            Value = $"Игр: {lines.Count} | K/D/A: {kills}/{deaths}/{assists}\nОчки: {score} | K/D: {kd} | KDA: {kda} | Ср.счёт: {avgScore}",
            Inline = false
        });

        // Избранная броня: показываем все сборки (боевая/скоростная/комбинированная)
        if (favorite is not null)
        {
            var variants = card.Armors.Where(a => a.ArmorId == favorite.ArmorId).ToList();
            foreach (var v in variants)
            {
                fields.Add(new EmbedFieldProperties
                {
                    Name = $"★ {v.Name} ({BuildName(v.BuildType)})",
                    Value = $"HP {v.EffectiveHealth:0.##} | Леч {v.HealPerSec:0.##} | Скор {v.MoveSpeed+v.RunSpeed:0.##} | Вес {v.CarryWeight:0.##}\nПул {v.BulletResist:0.##} | Разрыв {v.RuptureProtection:0.##} | Стойк {v.Resilience:0.##} | Вынос {v.Endurance:0.##}",
                    Inline = false
                });
            }
        }

        if (card.Builds.Count > 0)
        {
            var buildText = string.Join("\n", card.Builds.Select(b =>
                $"{BuildName(b.BuildType)}: HP {b.EffectiveHealth:0.##}, леч {b.HealPerSec:0.##}, скор {b.MoveSpeed+b.RunSpeed:0.##}, вес {b.CarryWeight:0.##}"));
            fields.Add(new EmbedFieldProperties { Name = "Сборки", Value = buildText, Inline = false });
        }

        var recent = lines.OrderByDescending(l => l.WeekStart).ThenByDescending(l => l.Stage).Take(10).ToList();
        if (recent.Count > 0)
        {
            var stageText = string.Join("\n", recent.Select(l =>
                $"{l.WeekStart:dd.MM} э{l.Stage}: {l.Kills}/{l.Deaths}/{l.Assists} · {l.Score} · {(l.IsExcused ? "отписка" : (l.IsWinner ? "победа" : "поражение"))}"));
            fields.Add(new EmbedFieldProperties { Name = "Последние этапы", Value = stageText, Inline = false });
        }

        var embed = new EmbedProperties
        {
            Title = $"📋 {card.Nickname}",
            Color = new Color(0x5865F2),
            Fields = fields
        };

        ArmorCardStorage.Cards[user.Id] = card;

        var armorOptions = card.Armors.Select((a, i) => new StringMenuSelectOptionProperties(
            (a.IsFavorite ? "★ " : "") + a.Name + $" ({BuildName(a.BuildType)})", i.ToString())).ToList();

        await ModifyResponseAsync(options =>
        {
            options.Flags = MessageFlags.Ephemeral;
            options.Embeds = [embed];
            if (armorOptions.Count > 0)
                options.Components = [new StringMenuProperties("armor_select", armorOptions)
                {
                    Placeholder = "Выбрать броню для подробностей",
                    MinValues = 1,
                    MaxValues = 1
                }];
        });
    }

    private static string StatusName(string status) => status switch
    {
        "in_clan" => "в клане",
        "on_leave" => "в отпуске",
        "excluded" => "исключен",
        _ => "в обработке"
    };

    private static string BuildName(string type) => type switch
    {
        "fat" => "боевая",
        "speed" => "скоростная",
        _ => "комбинированная"
    };
}
