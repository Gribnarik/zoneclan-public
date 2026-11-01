namespace ZoneClan.Core.Features.Settings.Models;

public static class ClanRoleSlots
{
    public const string Clan = "clan";
    public const string RankNew = "rank_new";
    public const string RankMid = "rank_mid";
    public const string RankVeteran = "rank_veteran";
    public const string Recruiter = "recruiter";
    public const string Duelist = "duelist";
    public const string Caller = "caller";
    public const string Timekeeper = "timekeeper";

    public static readonly IReadOnlyList<(string Code, string Title, string Column)> All =
    [
        (Clan, "Роль клана", "clan_role_id"),
        (RankNew, "Ранг: новичок", "rank_new_role_id"),
        (RankMid, "Ранг: освоился", "rank_mid_role_id"),
        (RankVeteran, "Ранг: ветеран", "rank_veteran_role_id"),
        (Recruiter, "Рекрутер", "recruiter_role_id"),
        (Duelist, "Дуэлянт", "duelist_role_id"),
        (Caller, "Коллер", "caller_role_id"),
        (Timekeeper, "Табельщик", "timekeeper_role_id")
    ];

    public static bool IsKnown(string code) => All.Any(slot => slot.Code == code);

    public static string Title(string code) =>
        All.FirstOrDefault(slot => slot.Code == code).Title ?? code;

    public static string Column(string code) =>
        All.First(slot => slot.Code == code).Column;
}

public class ClanSettingsDto
{
    public ulong GuildId { get; set; }
    public string ClanName { get; set; } = string.Empty;
    public string ClanTag { get; set; } = string.Empty;
    public string ClanDescription { get; set; } = string.Empty;
    public ulong? ClanRoleId { get; set; }
    public ulong? RankNewRoleId { get; set; }
    public ulong? RankMidRoleId { get; set; }
    public ulong? RankVeteranRoleId { get; set; }
    public ulong? RecruiterRoleId { get; set; }
    public ulong? DuelistRoleId { get; set; }
    public ulong? CallerRoleId { get; set; }
    public ulong? TimekeeperRoleId { get; set; }
    public ulong? ReminderChannelId { get; set; }
    public ulong? GatheringChannelId { get; set; }
    public ulong? PenaltyChannelId { get; set; }
    public ulong? AbsentChannelId { get; set; }

    public ulong? RoleIdFor(string slot) => slot switch
    {
        ClanRoleSlots.Clan => ClanRoleId,
        ClanRoleSlots.RankNew => RankNewRoleId,
        ClanRoleSlots.RankMid => RankMidRoleId,
        ClanRoleSlots.RankVeteran => RankVeteranRoleId,
        ClanRoleSlots.Recruiter => RecruiterRoleId,
        ClanRoleSlots.Duelist => DuelistRoleId,
        ClanRoleSlots.Caller => CallerRoleId,
        ClanRoleSlots.Timekeeper => TimekeeperRoleId,
        _ => null
    };
}

public class ClanProfileUpdate
{
    public string ClanName { get; set; } = string.Empty;
    public string ClanTag { get; set; } = string.Empty;
    public string ClanDescription { get; set; } = string.Empty;
    public ulong UpdatedBy { get; set; }
}

public class ClanRoleSlotUpdate
{
    public ulong? RoleId { get; set; }
}
