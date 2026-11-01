using System.Net.Http.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Core.Features.Settings.Models;

namespace ZoneClan.Bot.Features.Settings.UI;

public class SettingsButtonModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("settings_save_roles")]
    public async Task SaveRolesAsync()
    {
        if (!GuildAccess.CanManageGuild(Context.Guild, Context.User) || Context.Guild is null)
        {
            await RespondAsync(DiscordUi.PrivateText("Нужно право «Управлять сервером»."));
            return;
        }

        if (!SettingsStorage.Active.TryGetValue(Context.User.Id, out var draft))
        {
            await RespondAsync(DiscordUi.PrivateText("Откройте /настройки заново."));
            return;
        }

        using var client = httpClientFactory.CreateClient("api");
        var response = await client.PutAsJsonAsync(
            $"api/guilds/{Context.Guild.Id}/settings/roles/{draft.Slot}",
            new ClanRoleSlotUpdate { RoleId = draft.PendingRoleId });
        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(DiscordUi.PrivateText($"❌ {await response.Content.ReadAsStringAsync()}"));
            return;
        }

        var settings = await SettingsApi.GetAsync(client, Context.Guild.Id);
        await RespondAsync(InteractionCallback.ModifyMessage(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = SettingsUi.Root(settings, Context.Guild).ToArray();
        }));
    }

    [ComponentInteraction("settings_back")]
    public async Task BackAsync()
    {
        if (Context.Guild is null)
            return;

        using var client = httpClientFactory.CreateClient("api");
        var settings = await SettingsApi.GetAsync(client, Context.Guild.Id);
        SettingsStorage.Active.Remove(Context.User.Id);
        await RespondAsync(InteractionCallback.ModifyMessage(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = SettingsUi.Root(settings, Context.Guild).ToArray();
        }));
    }
}
