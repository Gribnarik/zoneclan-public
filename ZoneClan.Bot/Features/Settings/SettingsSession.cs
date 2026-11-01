namespace ZoneClan.Bot.Features.Settings;

public class SettingsDraft
{
    public string Slot { get; set; } = string.Empty;
    public ulong? PendingRoleId { get; set; }
}

public static class SettingsStorage
{
    public static Dictionary<ulong, SettingsDraft> Active { get; } = new();
}
