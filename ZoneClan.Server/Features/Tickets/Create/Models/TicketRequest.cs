namespace ZoneClan.Server.Features.Tickets.Create.Models;

public class TicketRequest
{
    public ulong DiscordId { get; set; }
    public string IngameName { get; set; } = string.Empty;
    public string DuelNote { get; set; } = string.Empty;
    public string PlayerNote { get; set; } = string.Empty;
    public string RawStatsText { get; set; } = string.Empty;
    public List<int> ArmorIds { get; set; } = new();
    public string BuildType { get; set; } = "combined";
    public List<TicketBuildRequest> Builds { get; set; } = new();
    public int FavoriteArmorId { get; set; }
    public string Status { get; set; } = "pending";
    public bool HasSledgehammer { get; set; }
    public bool PlaysSniper { get; set; }
    public List<int> SniperIds { get; set; } = new();
    public ulong SubmittedBy { get; set; }
}

public class TicketBuildRequest
{
    public string BuildType { get; set; } = "combined";
    public string RawStatsText { get; set; } = string.Empty;
    public List<int> ArmorIds { get; set; } = new();
}
