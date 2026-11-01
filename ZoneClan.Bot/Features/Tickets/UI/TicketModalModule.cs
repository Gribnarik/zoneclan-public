using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ZoneClan.Bot.Features.Tickets.UI;

public class TicketModalModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<ModalInteractionContext>
{
    [ComponentInteraction("ticket_modal_submit")]
    public async Task HandleModalSubmitAsync()
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("❌ Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        await RespondAsync(InteractionCallback.DeferredMessage(DiscordUi.PrivateV2));

        var textInputs = Context.Components
            .OfType<Label>()
            .Select(label => label.Component)
            .OfType<TextInput>()
            .ToList();

        var ingameName = textInputs.FirstOrDefault(input => input.CustomId == "ingame_name")?.Value ?? "";
        var duelNote = textInputs.FirstOrDefault(input => input.CustomId == "duel_note")?.Value ?? "";
        string Stats(string id) => textInputs.FirstOrDefault(input => input.CustomId == id)?.Value ?? "";

        var payload = new
        {
            DiscordId = session.TargetPlayerId,
            IngameName = ingameName,
            DuelNote = duelNote,
            PlayerNote = "",
            FavoriteArmorId = session.FavoriteArmorId,
            Builds = TicketUi.ChosenInOrder(session)
                .Select(type => new
                {
                    BuildType = type,
                    RawStatsText = Stats("stats_" + type),
                    ArmorIds = session.SelectedArmorIds.ToList()
                })
                .ToList(),
            Status = session.Status,
            HasSledgehammer = session.HasSledgehammer,
            PlaysSniper = session.PlaysSniper,
            SniperIds = session.SelectedSniperIds.ToList(),
            SubmittedBy = Context.User.Id
        };

        using var client = httpClientFactory.CreateClient("api");
        try
        {
            var response = await client.PostAsJsonAsync("api/tickets/create", payload);
            if (response.IsSuccessStatusCode)
            {
                await ModifyResponseAsync(options =>
                {
                    options.Flags = DiscordUi.PrivateV2;
                    options.Components = [new TextDisplayProperties("✅ Тикет сохранен!")];
                });
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                await ModifyResponseAsync(options =>
                {
                    options.Flags = DiscordUi.PrivateV2;
                    options.Components = [new TextDisplayProperties($"❌ Ошибка: {err}")];
                });
            }
        }
        catch (Exception ex)
        {
            await ModifyResponseAsync(options =>
            {
                options.Flags = DiscordUi.PrivateV2;
                options.Components = [new TextDisplayProperties($"❌ Сеть: {ex.Message}")];
            });
        }

        TicketStorage.ActiveSessions.Remove(Context.User.Id);
    }
}
