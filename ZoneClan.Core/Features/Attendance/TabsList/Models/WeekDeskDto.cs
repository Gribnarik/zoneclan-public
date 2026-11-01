using ZoneClan.Core.Features.Attendance.Shared;

namespace ZoneClan.Core.Features.Attendance.TabsList.Models;

public class ClanPlayerDto
{
    public ulong PlayerId { get; set; }
    public string Nickname { get; set; } = "";
}

public class TabPlayerRowDto
{
    public int Id { get; set; }
    public ulong? PlayerId { get; set; }
    public string Nickname { get; set; } = "";
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Score { get; set; }
    public bool IsExcused { get; set; }
    public string Status { get; set; } = "";
    public double Kd => CombatMath.Kd(Kills, Deaths);
}

public class StageColumnDto
{
    public int Stage { get; set; }
    public int? TabId { get; set; }
    public string Team1Name { get; set; } = "";
    public int Team1Score { get; set; }
    public string Team2Name { get; set; } = "";
    public int Team2Score { get; set; }
    public bool IsValid { get; set; }
    public bool IsWinner { get; set; }
    public int Guilds { get; set; }
    public List<TabPlayerRowDto> Players { get; set; } = [];
    public List<ClanPlayerDto> Missed { get; set; } = [];
}

public class WeekSummaryDto
{
    public int Attended { get; set; }
    public int Excused { get; set; }
    public int Missed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Guilds { get; set; }
    public double Kd => CombatMath.Kd(Kills, Deaths);
}

public class WeekDeskDto
{
    public DateOnly WeekStart { get; set; }
    public WeekSummaryDto Summary { get; set; } = new();
    public List<StageColumnDto> Stages { get; set; } = [];
}

public class UpdatePlayerRowRequest
{
    public bool IsExcused { get; set; }
    public string Status { get; set; } = "";
}
