using ZoneClan.Core.Features.Attendance.Shared;

namespace ZoneClan.Core.Features.Attendance.PlayerStats.Models;

public class PlayerListRowDto
{
    public ulong? PlayerId { get; set; }
    public string Nickname { get; set; } = "";
    public string Status { get; set; } = "";
    public int Games { get; set; }
    public int Attended { get; set; }
    public int Excused { get; set; }
    public int Missed { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Score { get; set; }
    public double Kd => CombatMath.Kd(Kills, Deaths);
    public int Characteristic { get; set; }
    public double Kda => Deaths == 0 ? Kills + Assists : (double)(Kills + Assists) / Deaths;
}
