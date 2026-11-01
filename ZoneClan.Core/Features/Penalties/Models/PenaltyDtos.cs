namespace ZoneClan.Core.Features.Penalties.Models;

public class PenaltyTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int DefaultPoints { get; set; }
}

public class PenaltyDto
{
    public int Id { get; set; }
    public ulong PlayerId { get; set; }
    public int TypeId { get; set; }
    public string Nickname { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string Reason { get; set; } = "";
    public int Points { get; set; }
    public string Status { get; set; } = "active";
    public string? ResolutionMethod { get; set; }
    public DateTime IssuedAt { get; set; }
}

public class PenaltySummaryDto
{
    public ulong PlayerId { get; set; }
    public string Nickname { get; set; } = "";
    public int TotalPoints { get; set; }
    public int ActiveCount { get; set; }
}

public class PenaltyPageDto
{
    public List<PenaltyTypeDto> Types { get; set; } = [];
    public List<PenaltyDto> Items { get; set; } = [];
    public List<PenaltySummaryDto> Summaries { get; set; } = [];
}

public class IssuePenaltyRequest
{
    public ulong PlayerId { get; set; }
    public int TypeId { get; set; }
    public string Reason { get; set; } = "";
    public int Points { get; set; }
}

public class UpdatePenaltyRequest
{
    public int TypeId { get; set; }
    public string Reason { get; set; } = "";
    public int Points { get; set; }
}

public class ResolvePenaltyRequest
{
    public string Method { get; set; } = "worked_off";
}
