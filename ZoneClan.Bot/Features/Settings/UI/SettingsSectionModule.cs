using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Core.Features.Settings.Models;

namespace ZoneClan.Bot.Features.Settings.UI;

public class SettingsSectionModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("settings_section")]
    public async Task HandleSectionAsync()
    {
        if (!GuildAccess.CanManageGuild(Context.Guild, Context.User))
        {
            await RespondAsync(DiscordUi.PrivateText("Нужно право «Управлять сервером»."));
            return;
        }

        var value = Context.SelectedValues.FirstOrDefault();
        if (string.IsNullOrEmpty(value) || Context.Guild is null)
            return;

        using var client = httpClientFactory.CreateClient("api");
        var settings = await SettingsApi.GetAsync(client, Context.Guild.Id);

        if (value == "profile")
        {
            await RespondAsync(InteractionCallback.Modal(SettingsUi.ProfileModal(settings)));
            return;
        }

        if (value is "gathering_channel" or "penalty_channel" or "absent_channel")
        {
            var title = value switch
            {
                "gathering_channel" => "Канал КВ (сборы)",
                "penalty_channel" => "Канал штрафов",
                _ => "Канал отсутствующих"
            };
            var customId = value switch
            {
                "gathering_channel" => "settings_gathering_channel",
                "penalty_channel" => "settings_penalty_channel",
                _ => "settings_absent_channel"
            };
            await RespondAsync(InteractionCallback.ModifyMessage(options =>
            {
                options.Flags = MessageFlags.IsComponentsV2;
                options.Components =
                [
                    new TextDisplayProperties($"📢 **{title}**\nВыберите канал."),
                    new ChannelMenuProperties(customId)
                    {
                        Placeholder = "Канал",
                        ChannelTypes = [ChannelType.TextGuildChannel],
                        MinValues = 1,
                        MaxValues = 1
                    },
                    new ActionRowProperties([new ButtonProperties("settings_back", "Назад", ButtonStyle.Secondary)])
                ];
            }));
            return;
        }

        if (!value.StartsWith("role:", StringComparison.Ordinal))
            return;

        var slot = value["role:".Length..];
        if (!ClanRoleSlots.IsKnown(slot))
            return;

        var current = settings.RoleIdFor(slot);
        SettingsStorage.Active[Context.User.Id] = new SettingsDraft
        {
            Slot = slot,
            PendingRoleId = current
        };

        await RespondAsync(InteractionCallback.ModifyMessage(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = SettingsUi.RoleEditor(slot, current).ToArray();
        }));
    }
}
