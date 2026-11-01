using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace ZoneClan.Bot.Features.Tickets;

public class TicketCommands(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("заявка", "Начать заполнение тикета на игрока")]
    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    public async Task StartTicketAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(DiscordUi.PrivateV2));

        using var client = httpClientFactory.CreateClient("api");
        var settings = await SettingsApi.GetAsync(client, Context.Guild!.Id);

        if (!GuildAccess.HasStaffRole(Context.Guild, Context.User, settings.DuelistRoleId))
        {
            await ModifyResponseAsync(options =>
            {
                options.Flags = DiscordUi.PrivateV2;
                options.Components = [new TextDisplayProperties("❌ Заявки может создавать только роль дуэлянта.")];
            });
            return;
        }

        await ModifyResponseAsync(options =>
        {
            options.Flags = DiscordUi.PrivateV2;
            options.Components = TicketUi.PlayerStep().ToArray();
        });
    }
}
