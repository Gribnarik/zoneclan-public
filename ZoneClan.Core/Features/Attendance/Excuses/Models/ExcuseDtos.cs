namespace ZoneClan.Core.Features.Attendance.Excuses.Models;

public class ExcuseTabDto
{
    public int TabId { get; set; }
    public DateOnly WeekStart { get; set; }
    public int Stage { get; set; }
    public DateOnly StageDate { get; set; }
}

public class ExcusePlayerDto
{
    public ulong PlayerId { get; set; }
    public string Nickname { get; set; } = "";
}

public class ExcuseCellDto
{
    public int TabId { get; set; }
    public DateOnly WeekStart { get; set; }
    public int Stage { get; set; }
    public ulong PlayerId { get; set; }
    public string Status { get; set; } = "missed";
}

public class ExcuseMatrixDto
{
    public List<ExcuseTabDto> Tabs { get; set; } = [];
    public List<ExcusePlayerDto> Players { get; set; } = [];
    public List<ExcuseCellDto> Cells { get; set; } = [];
}

public class SetExcuseRequest
{
    public ulong PlayerId { get; set; }
    public string Status { get; set; } = "missed";
}

public class BulkExcuseRequest
{
    public int TabId { get; set; }
    public List<ulong> PlayerIds { get; set; } = [];
    public string Status { get; set; } = "missed";
}