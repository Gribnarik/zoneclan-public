using ZoneClan.Core.Features.Roster.Models;

namespace ZoneClan.Bot.Features.Stats;

public static class ArmorCardStorage
{
    public static readonly Dictionary<ulong, RosterCardDto> Cards = new();
}
