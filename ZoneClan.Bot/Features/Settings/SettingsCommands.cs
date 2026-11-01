using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace ZoneClan.Bot.Features.Settings;

public class SettingsCommands(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("настройки", "Имя клана, роль клана, ранги и штаб")]
    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageGuild)]
    public async Task OpenSettingsAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(DiscordUi.PrivateV2));

        using var client = httpClientFactory.CreateClient("api");
        var settings = await SettingsApi.GetAsync(client, Context.Guild!.Id);
        await ModifyResponseAsync(options =>
        {
            options.Flags = DiscordUi.PrivateV2;
            options.Components = SettingsUi.Root(settings, Context.Guild).ToArray();
        });
    }
}
