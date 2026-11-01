namespace ZoneClan.Web.Features.Roster.Validation;

public static class RosterValidation
{
    public static bool IsValidDiscordId(ulong id) => id > 0;
    public static bool IsValidNickname(string? nickname) => !string.IsNullOrWhiteSpace(nickname);
}
