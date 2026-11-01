namespace ZoneClan.Bot.Features.Tickets;

using ZoneClan.Core.Features.Tickets.Models;

public class TicketSession
{
    public ulong TargetPlayerId { get; set; }
    public string TargetPlayerName { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public string IngameName { get; set; } = string.Empty;
    public string DuelNote { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string BuildType { get; set; } = "combined";
    public HashSet<string> ChosenBuilds { get; set; } = new();
    public bool HasSledgehammer { get; set; }
    public bool PlaysSniper { get; set; }
    public int FavoriteArmorId { get; set; }
    public HashSet<string> FilledBuilds { get; set; } = new();
    public Dictionary<string, HashSet<int>> ArmorsByBuild { get; set; } = new();
    public HashSet<int> SelectedSniperIds { get; set; } = new();
    public HashSet<int> SelectedArmorIds { get; set; } = new();
    public List<SniperInfo> Snipers { get; set; } = new();
}

public static class TicketStorage
{
    public static Dictionary<ulong, TicketSession> ActiveSessions { get; } = new();
}
