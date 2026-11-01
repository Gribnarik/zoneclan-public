namespace ZoneClan.Core.Features.Attendance.UploadTab.Models;

public class AttendancePlayerDraft
{
    public string Nickname { get; set; } = "";
    public ulong? PlayerId { get; set; }
    public bool Known { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Score { get; set; }
}

public class KnownNickDto
{
    public ulong DiscordId { get; set; }
    public string Nickname { get; set; } = "";
}

public class AttendancePreviewDto
{
    public string DraftId { get; set; } = "";
    public DateOnly WeekStart { get; set; }
    public int DayOfWeek { get; set; } 
    public int Stage { get; set; }
    public string Team1Name { get; set; } = "";
    public int Team1Score { get; set; }
    public string Team2Name { get; set; } = "";
    public int Team2Score { get; set; }
    public int Guilds { get; set; }
    public bool ClanRecognized { get; set; }
    public bool IsWinner { get; set; }
    public string Message { get; set; } = "";
    public List<AttendancePlayerDraft> Players { get; set; } = [];
    public List<KnownNickDto> KnownPlayers { get; set; } = [];
}

public class SaveTabRequest
{
    public string DraftId { get; set; } = "";
    public DateOnly WeekStart { get; set; }
    public int DayOfWeek { get; set; }
    public int Stage { get; set; }
    public string Team1Name { get; set; } = "";
    public int Team1Score { get; set; }
    public string Team2Name { get; set; } = "";
    public int Team2Score { get; set; }
    public int Guilds { get; set; }
    public List<AttendancePlayerDraft> Players { get; set; } = [];
}
