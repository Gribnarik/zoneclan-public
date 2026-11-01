using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Core.Features.Tickets.Models;

namespace ZoneClan.Bot.Features.Tickets.UI;

public class TicketArmorMenuModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("ticket_armor_cat")]
    public async Task HandleArmorCategoryAsync(int categoryId)
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        await RespondAsync(InteractionCallback.DeferredModifyMessage);

        using var client = httpClientFactory.CreateClient("api");
        var armors = await client.GetFromJsonAsync<List<ArmorInfo>>("api/armors") ?? [];
        var categoryArmorIds = armors
            .Where(armor => armor.CategoryId == categoryId)
            .Select(armor => armor.Id)
            .ToHashSet();

        session.SelectedArmorIds.RemoveWhere(id => categoryArmorIds.Contains(id));
        foreach (var value in Context.SelectedValues)
        {
            if (int.TryParse(value, out var armorId))
                session.SelectedArmorIds.Add(armorId);
        }

        if (!session.SelectedArmorIds.Contains(session.FavoriteArmorId))
            session.FavoriteArmorId = session.SelectedArmorIds.FirstOrDefault();

        await ShowArmorAsync(session, armors);
    }

    [ComponentInteraction("ticket_favorite")]
    public async Task HandleFavoriteAsync()
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        await RespondAsync(InteractionCallback.DeferredModifyMessage);

        session.FavoriteArmorId = Context.SelectedValues
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .FirstOrDefault(id => id > 0);

        using var client = httpClientFactory.CreateClient("api");
        var armors = await client.GetFromJsonAsync<List<ArmorInfo>>("api/armors") ?? [];
        await ShowArmorAsync(session, armors);
    }

    private Task ShowArmorAsync(TicketSession session, IReadOnlyList<ArmorInfo> armors) =>
        ModifyResponseAsync(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = TicketUi.ArmorStep(armors, session).ToArray();
        });
}
