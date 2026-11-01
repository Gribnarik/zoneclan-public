namespace ZoneClan.Core.Features.Attendance.PlayerStats.Models;

public class PlayerTabLineDto
{
    public int TabId { get; set; }
    public DateOnly WeekStart { get; set; }
    public int Stage { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Score { get; set; }
    public bool IsExcused { get; set; }
    public bool IsWinner { get; set; }
}

public class PlayerCardDto
{
    public ulong? PlayerId { get; set; }
    public string Nickname { get; set; } = "";
    public List<PlayerTabLineDto> Lines { get; set; } = [];
}
