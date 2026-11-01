using ZoneClan.Core.Features.Attendance.PlayerStats.Models;
using ZoneClan.Core.Features.Attendance.Shared;
using ZoneClan.Core.Features.Roles.Models;

namespace ZoneClan.Core.Features.Roster.Models;

public class RosterPlayerDto
{
    public ulong DiscordId { get; set; }
    public string Nickname { get; set; } = "";
    public string Status { get; set; } = "";
    public bool HasSledgehammer { get; set; }
    public bool PlaysSniper { get; set; }
    public int? SquadId { get; set; }
    public string SquadName { get; set; } = "";
    public List<RoleDto> Roles { get; set; } = [];
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Score { get; set; }
    public double Kd => CombatMath.Kd(Kills, Deaths);
    public FavoriteArmorDto? Favorite { get; set; }
}

public class FavoriteArmorDto
{
    public string Name { get; set; } = "";
    public List<FavoriteLoadoutDto> Loadouts { get; set; } = [];
}

public class FavoriteLoadoutDto
{
    public string BuildType { get; set; } = "";
    public decimal Health { get; set; }
    public decimal HealPerSecond { get; set; }
    public decimal Speed { get; set; }
    public decimal Weight { get; set; }
    public decimal EnduranceRegen { get; set; }
    public decimal Resilience { get; set; }
}

public class RosterNoteDto
{
    public string NoteType { get; set; } = "";
    public string Content { get; set; } = "";
}

public class RosterBuildDto
{
    public string BuildType { get; set; } = "combined";
    public decimal EffectiveHealth { get; set; }
    public decimal HealPerSec { get; set; }
    public decimal Bleeding { get; set; }
    public decimal Vitality { get; set; }
    public decimal HealEfficiency { get; set; }
    public decimal HealthRegen { get; set; }
    public decimal MoveSpeed { get; set; }
    public decimal RunSpeed { get; set; }
    public decimal Endurance { get; set; }
    public decimal EnduranceRegen { get; set; }
    public decimal CarryWeight { get; set; }
    public decimal BulletResist { get; set; }
    public decimal RuptureProtection { get; set; }
    public decimal Resilience { get; set; }
}

public class RosterArmorDto : RosterBuildDto
{
    public int ArmorId { get; set; }
    public string Name { get; set; } = "";
    public decimal TotalSpeed { get; set; }
    public bool IsFavorite { get; set; }
}

public class StatModifierDto
{
    public string Code { get; set; } = "";
    public decimal Value { get; set; }
}

public class CatalogItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Selected { get; set; }
    public decimal BulletResist { get; set; }
    public decimal RuptureProtection { get; set; }
    public List<StatModifierDto> Modifiers { get; set; } = [];
}

public class RosterCardDto
{
    public ulong DiscordId { get; set; }
    public string Nickname { get; set; } = "";
    public string Status { get; set; } = "";
    public bool HasSledgehammer { get; set; }
    public bool PlaysSniper { get; set; }
    public string Note { get; set; } = "";
    public string DuelNote { get; set; } = "";
    public List<RosterNoteDto> Notes { get; set; } = [];
    public List<RoleDto> Roles { get; set; } = [];
    public List<RoleDto> RoleCatalog { get; set; } = [];
    public RosterBuildDto Build { get; set; } = new();
    public List<RosterBuildDto> Builds { get; set; } = [];
    public List<RosterArmorDto> Armors { get; set; } = [];
    public List<CatalogItemDto> ArmorCatalog { get; set; } = [];
    public List<CatalogItemDto> Snipers { get; set; } = [];
    public List<PlayerTabLineDto> Lines { get; set; } = [];
}

public class UpdateRosterRequest
{
    public ulong DiscordId { get; set; }
    public string Nickname { get; set; } = "";
    public string Status { get; set; } = "pending";
    public bool HasSledgehammer { get; set; }
    public bool PlaysSniper { get; set; }
    public string Note { get; set; } = "";
    public List<RosterNoteDto> Notes { get; set; } = [];
    public RosterBuildDto Build { get; set; } = new();
    public List<RosterBuildDto> Builds { get; set; } = [];
    public List<RosterArmorDto> Armors { get; set; } = [];
    public List<int> SniperIds { get; set; } = [];
    public List<int> RoleIds { get; set; } = [];
}

public class RosterSaveResult
{
    public ulong DiscordId { get; set; }
}
