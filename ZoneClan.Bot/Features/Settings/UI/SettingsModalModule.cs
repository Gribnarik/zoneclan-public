using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Core.Features.Settings.Models;

namespace ZoneClan.Bot.Features.Settings.UI;

public class SettingsModalModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<ModalInteractionContext>
{
    [ComponentInteraction("settings_profile_modal")]
    public async Task HandleProfileAsync()
    {
        if (!GuildAccess.CanManageGuild(Context.Guild, Context.User) || Context.Guild is null)
        {
            await RespondAsync(DiscordUi.PrivateText("Нужно право «Управлять сервером»."));
            return;
        }

        var inputs = Context.Components.OfType<Label>().Select(label => label.Component).OfType<TextInput>().ToList();
        var payload = new ClanProfileUpdate
        {
            ClanName = inputs.FirstOrDefault(input => input.CustomId == "clan_name")?.Value ?? "",
            ClanTag = inputs.FirstOrDefault(input => input.CustomId == "clan_tag")?.Value ?? "",
            ClanDescription = inputs.FirstOrDefault(input => input.CustomId == "clan_description")?.Value ?? "",
            UpdatedBy = Context.User.Id
        };

        using var client = httpClientFactory.CreateClient("api");
        var response = await client.PutAsJsonAsync($"api/guilds/{Context.Guild.Id}/settings/profile", payload);
        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(DiscordUi.PrivateText($"❌ {await response.Content.ReadAsStringAsync()}"));
            return;
        }

        var settings = await SettingsApi.GetAsync(client, Context.Guild.Id);
        await RespondAsync(DiscordUi.PrivateMessage(SettingsUi.Root(settings, Context.Guild).ToArray()));
    }
}
