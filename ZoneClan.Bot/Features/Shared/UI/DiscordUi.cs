using NetCord;
using NetCord.Rest;

namespace ZoneClan.Bot.Features.Shared.UI;

internal static class DiscordUi
{
    public static MessageFlags PrivateV2 => MessageFlags.IsComponentsV2 | MessageFlags.Ephemeral;

    public static InteractionCallbackProperties PrivateMessage(params IMessageComponentProperties[] components) =>
        InteractionCallback.Message(new InteractionMessageProperties
        {
            Flags = PrivateV2,
            Components = components
        });

    public static InteractionCallbackProperties PrivateText(string text) =>
        InteractionCallback.Message(new InteractionMessageProperties
        {
            Flags = MessageFlags.Ephemeral,
            Content = text
        });
}
