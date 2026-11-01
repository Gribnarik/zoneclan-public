using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using ZoneClan.Core.Features.Settings.Models;

namespace ZoneClan.Bot.Features.Settings.UI;

internal static class SettingsUi
{
    public static IEnumerable<IMessageComponentProperties> Root(ClanSettingsDto settings, Guild? guild)
    {
        var name = string.IsNullOrWhiteSpace(settings.ClanName) ? "не задано" : settings.ClanName;
        yield return new TextDisplayProperties(
            $"⚙️ **Настройки клана**\n**Имя:** {name}\nПотом по имени сделаем табы. Участнику выдаётся роль клана + один ранг, например Люмос + новичок.\n\n{FormatRoles(settings, guild)}\n\n{FormatChannels(settings)}");

        var options = new List<StringMenuSelectOptionProperties>
        {
            new("Имя и профиль клана", "profile")
        };
        options.AddRange(ClanRoleSlots.All.Select(slot =>
            new StringMenuSelectOptionProperties(slot.Title, $"role:{slot.Code}")));
        options.Add(new StringMenuSelectOptionProperties("Канал КВ (сборы)", "gathering_channel"));
        options.Add(new StringMenuSelectOptionProperties("Канал штрафов", "penalty_channel"));
        options.Add(new StringMenuSelectOptionProperties("Канал отсутствующих", "absent_channel"));

        yield return new StringMenuProperties("settings_section", options)
        {
            Placeholder = "Что настроить...",
            MinValues = 1,
            MaxValues = 1
        };
    }

    public static IEnumerable<IMessageComponentProperties> RoleEditor(string slot, ulong? selectedRoleId)
    {
        var current = selectedRoleId is > 0 ? $"<@&{selectedRoleId}>" : "не задана";
        yield return new TextDisplayProperties(
            $"🔐 **{ClanRoleSlots.Title(slot)}**\nСейчас: {current}\nВыберите одну роль и нажмите Сохранить. Пустой выбор сбрасывает слот.");
        yield return new RoleMenuProperties("settings_roles")
        {
            Placeholder = ClanRoleSlots.Title(slot),
            MinValues = 0,
            MaxValues = 1,
            DefaultValues = selectedRoleId is > 0 ? [selectedRoleId.Value] : []
        };
        yield return new ActionRowProperties(
        [
            new ButtonProperties("settings_save_roles", "Сохранить", ButtonStyle.Success),
            new ButtonProperties("settings_back", "Назад", ButtonStyle.Secondary)
        ]);
    }

    public static ModalProperties ProfileModal(ClanSettingsDto settings) =>
        new("settings_profile_modal", "Профиль клана")
        {
            Components =
            [
                new LabelProperties("Имя клана", new TextInputProperties("clan_name", TextInputStyle.Short)
                {
                    Required = true,
                    MaxLength = 64,
                    Value = settings.ClanName
                }),
                new LabelProperties("Тег", new TextInputProperties("clan_tag", TextInputStyle.Short)
                {
                    Required = false,
                    MaxLength = 16,
                    Value = settings.ClanTag
                }),
                new LabelProperties("Описание", new TextInputProperties("clan_description", TextInputStyle.Paragraph)
                {
                    Required = false,
                    Value = settings.ClanDescription
                })
            ]
        };

    private static string FormatChannels(ClanSettingsDto settings)
    {
        static string Ch(ulong? id) => id is > 0 ? $"<#{id}>" : "—";
        return string.Join('\n',
        [
            $"📢 Канал КВ (сборы): {Ch(settings.GatheringChannelId)}",
            $"📢 Канал штрафов: {Ch(settings.PenaltyChannelId)}",
            $"📢 Канал отсутствующих: {Ch(settings.AbsentChannelId)}"
        ]);
    }

    private static string FormatRoles(ClanSettingsDto settings, Guild? guild)
    {
        static string Name(Guild? currentGuild, ulong? id)
        {
            if (id is not > 0)
                return "—";
            if (currentGuild?.Roles.TryGetValue(id.Value, out var role) == true)
                return role.Name;
            return $"<@&{id}>";
        }

        return string.Join('\n', ClanRoleSlots.All.Select(slot =>
            $"• {slot.Title}: {Name(guild, settings.RoleIdFor(slot.Code))}"));
    }
}
