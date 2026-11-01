using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Core.Features.Roster.BusinessLogic;
using ZoneClan.Core.Features.Roster.Models;
using ZoneClan.Core.Features.Tickets.Models;

namespace ZoneClan.Bot.Features.Tickets.UI;

public class TicketUserMenuModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<UserMenuInteractionContext>
{
    [ComponentInteraction("ticket_select_user")]
    public async Task HandleUserSelectAsync()
    {
        var selectedUser = Context.SelectedValues.FirstOrDefault();
        if (selectedUser is null)
            return;

        await RespondAsync(InteractionCallback.DeferredModifyMessage);

        var session = new TicketSession
        {
            TargetPlayerId = selectedUser.Id,
            TargetPlayerName = selectedUser.Username
        };
        TicketStorage.ActiveSessions[Context.User.Id] = session;

        using var client = httpClientFactory.CreateClient("api");
        try
        {
            var cardResponse = await client.GetAsync($"api/roster/{selectedUser.Id}");
            if (cardResponse.IsSuccessStatusCode)
            {
                var card = await cardResponse.Content.ReadFromJsonAsync<RosterCardDto>();
                if (card is not null)
                {
                    session.Exists = true;
                    session.IngameName = card.Nickname;
                    session.DuelNote = card.DuelNote;
                    session.Status = card.Status is "in_clan" or "on_leave" or "excluded" ? card.Status : "pending";
                    session.HasSledgehammer = card.HasSledgehammer;
                    session.PlaysSniper = card.PlaysSniper;
                    session.FilledBuilds = card.Builds.Where(ArmorMath.HasStats).Select(build => build.BuildType).ToHashSet();
                    session.ChosenBuilds = new HashSet<string>(session.FilledBuilds);
                    session.BuildType = session.FilledBuilds.Contains("combined")
                        ? "combined"
                        : session.FilledBuilds.FirstOrDefault() ?? "combined";
                    session.SelectedArmorIds = card.Armors.Select(armor => armor.ArmorId).ToHashSet();
                    session.FavoriteArmorId = card.Armors.FirstOrDefault(armor => armor.IsFavorite)?.ArmorId ?? 0;
                    session.SelectedSniperIds = card.Snipers.Where(sniper => sniper.Selected).Select(sniper => sniper.Id).ToHashSet();
                }
            }
            else if (cardResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(await cardResponse.Content.ReadAsStringAsync());
            }

            session.Snipers = await client.GetFromJsonAsync<List<SniperInfo>>("api/snipers") ?? [];
        }
        catch (Exception ex)
        {
            await ModifyResponseAsync(options =>
            {
                options.Flags = MessageFlags.IsComponentsV2;
                options.Components = [new TextDisplayProperties($"❌ Не удалось загрузить карточку: {ex.Message}")];
            });
            return;
        }

        await ModifyResponseAsync(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = TicketUi.ProfileStep(session).ToArray();
        });
    }
}
