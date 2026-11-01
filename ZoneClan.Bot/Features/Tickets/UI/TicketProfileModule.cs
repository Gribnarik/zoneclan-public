using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Core.Features.Tickets.Models;

namespace ZoneClan.Bot.Features.Tickets.UI;

public class TicketProfileModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("ticket_status_pending")]
    public Task PendingAsync() => SetStatusAsync("pending");

    [ComponentInteraction("ticket_status_clan")]
    public Task ClanAsync() => SetStatusAsync("in_clan");

    [ComponentInteraction("ticket_status_leave")]
    public Task LeaveAsync() => SetStatusAsync("on_leave");

    [ComponentInteraction("ticket_status_excluded")]
    public Task ExcludedAsync() => SetStatusAsync("excluded");

    [ComponentInteraction("ticket_toggle_sledge")]
    public Task SledgeAsync() => ToggleAsync(session => session.HasSledgehammer = !session.HasSledgehammer);

    [ComponentInteraction("ticket_toggle_sniper")]
    public Task SniperAsync() => ToggleAsync(session =>
    {
        session.PlaysSniper = !session.PlaysSniper;
        if (!session.PlaysSniper)
            session.SelectedSniperIds.Clear();
    });

    [ComponentInteraction("ticket_profile_next")]
    public async Task NextAsync()
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        var chosen = TicketUi.ChosenInOrder(session);
        if (chosen.Count == 0)
        {
            await RespondAsync(DiscordUi.PrivateText("Отметьте хотя бы одну сборку."));
            return;
        }

        await RespondAsync(InteractionCallback.DeferredModifyMessage);

        using var client = httpClientFactory.CreateClient("api");
        IReadOnlyList<ArmorInfo> armors;
        try
        {
            armors = await client.GetFromJsonAsync<List<ArmorInfo>>("api/armors") ?? [];
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"❌ Не удалось загрузить брони с сервера: {ex.Message}");
            return;
        }

        if (armors.Count == 0)
        {
            await ShowErrorAsync("❌ Сервер вернул пустой список броней.");
            return;
        }

        await ModifyResponseAsync(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = TicketUi.ArmorStep(armors, session).ToArray();
        });
    }

    private Task SetStatusAsync(string status) =>
        ToggleAsync(session => session.Status = status);

    private async Task ToggleAsync(Action<TicketSession> change)
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        change(session);
        await RespondAsync(InteractionCallback.ModifyMessage(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = TicketUi.ProfileStep(session).ToArray();
        }));
    }

    private Task ShowErrorAsync(string text) =>
        ModifyResponseAsync(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = [new TextDisplayProperties(text)];
        });
}

public class TicketSniperMenuModule : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("ticket_snipers")]
    public async Task HandleSnipersAsync()
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        session.SelectedSniperIds = Context.SelectedValues
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        await RespondAsync(InteractionCallback.ModifyMessage(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = TicketUi.ProfileStep(session).ToArray();
        }));
    }
}

public class TicketBuildMenuModule : ComponentInteractionModule<StringMenuInteractionContext>
{
    private static readonly HashSet<string> BuildTypes = ["fat", "speed", "combined"];

    [ComponentInteraction("ticket_builds")]
    public async Task HandleBuildsAsync()
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        session.ChosenBuilds = Context.SelectedValues.Where(BuildTypes.Contains).ToHashSet();
        await RespondAsync(InteractionCallback.ModifyMessage(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = TicketUi.ProfileStep(session).ToArray();
        }));
    }
}
