using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Core.Features.Tickets.Models;

namespace ZoneClan.Bot.Features.Tickets.UI;

public class TicketBuildModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("ticket_build_fat")]
    public Task FatAsync() => ChooseAsync("fat");

    [ComponentInteraction("ticket_build_speed")]
    public Task SpeedAsync() => ChooseAsync("speed");

    [ComponentInteraction("ticket_build_combined")]
    public Task CombinedAsync() => ChooseAsync("combined");

    [ComponentInteraction("ticket_build_next")]
    public async Task NextAsync()
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
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

    private async Task ChooseAsync(string buildType)
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        session.ArmorsByBuild[session.BuildType] = new HashSet<int>(session.SelectedArmorIds);
        session.BuildType = buildType;
        session.SelectedArmorIds = session.ArmorsByBuild.TryGetValue(buildType, out var ids)
            ? new HashSet<int>(ids)
            : [];

        await RespondAsync(InteractionCallback.ModifyMessage(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = TicketUi.BuildStep(session).ToArray();
        }));
    }

    private Task ShowErrorAsync(string text) =>
        ModifyResponseAsync(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = [new TextDisplayProperties(text)];
        });
}
