using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ZoneClan.Bot.Features.Tickets.UI;

public class TicketArmorButtonModule : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("ticket_armor_done")]
    public async Task HandleArmorDoneAsync()
    {
        if (!TicketStorage.ActiveSessions.TryGetValue(Context.User.Id, out var session))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия заявки истекла. Начните заново командой /заявка."));
            return;
        }

        await RespondAsync(InteractionCallback.Modal(TicketUi.TicketModal(session)));
    }
}
