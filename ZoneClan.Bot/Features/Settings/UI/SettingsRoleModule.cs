using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace ZoneClan.Bot.Features.Settings.UI;

public class SettingsRoleModule : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [ComponentInteraction("settings_roles")]
    public async Task HandleRolesAsync()
    {
        if (!SettingsStorage.Active.TryGetValue(Context.User.Id, out var draft))
        {
            await RespondAsync(DiscordUi.PrivateText("Откройте /настройки заново."));
            return;
        }

        draft.PendingRoleId = Context.SelectedValues.FirstOrDefault()?.Id;
        await RespondAsync(InteractionCallback.ModifyMessage(options =>
        {
            options.Flags = MessageFlags.IsComponentsV2;
            options.Components = SettingsUi.RoleEditor(draft.Slot, draft.PendingRoleId).ToArray();
        }));
    }
}
