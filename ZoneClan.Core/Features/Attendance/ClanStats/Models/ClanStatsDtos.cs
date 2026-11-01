using ZoneClan.Core.Features.Attendance.Shared;

namespace ZoneClan.Core.Features.Attendance.ClanStats.Models;

public class TopPlayerDto
{
    public ulong? PlayerId { get; set; }
    public string Nickname { get; set; } = "";
    public double Value { get; set; }
}

public class WeekStatsDto
{
    public DateOnly WeekStart { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Score { get; set; }
    public int Guilds { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public double Kd => CombatMath.Kd(Kills, Deaths);
}

public class ClanStatsDto
{
    public int TotalGames { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate => TotalGames == 0 ? 0 : Math.Round(Wins * 100.0 / TotalGames, 1);
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Score { get; set; }
    public int Guilds { get; set; }
    public double Kd => CombatMath.Kd(Kills, Deaths);
    public List<TopPlayerDto> TopByKd { get; set; } = [];
    public List<TopPlayerDto> LowByKd { get; set; } = [];
    public List<TopPlayerDto> LowByKda { get; set; } = [];
    public List<TopPlayerDto> TopByKda { get; set; } = [];
    public List<TopPlayerDto> TopByScore { get; set; } = [];
    public List<TopPlayerDto> LowByScore { get; set; } = [];
    public List<TopPlayerDto> TopByAttendance { get; set; } = [];
    public List<TopPlayerDto> LowByAttendance { get; set; } = [];
    public List<TopPlayerDto> LowByMissed { get; set; } = [];
    public List<WeekStatsDto> Weeks { get; set; } = [];
}
