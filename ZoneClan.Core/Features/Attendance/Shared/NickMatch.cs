namespace ZoneClan.Core.Features.Attendance.Shared;

public static class NickMatch
{
    public static ulong? FindPlayerId(string input, IReadOnlyList<(ulong Id, string Nickname)> roster)
    {
        if (string.IsNullOrWhiteSpace(input) || roster.Count == 0)
            return null;

        var nick = input.Trim();
        var exact = roster.FirstOrDefault(player =>
            player.Nickname.Equals(nick, StringComparison.OrdinalIgnoreCase));
        if (exact.Id != 0)
            return exact.Id;

        var best = roster
            .Select(player => (player.Id, Score: Ratio(nick, player.Nickname)))
            .OrderByDescending(item => item.Score)
            .First();
        return best.Score >= 60 ? best.Id : null;
    }

    private static int Ratio(string left, string right)
    {
        if (left.Length == 0 && right.Length == 0)
            return 100;
        var max = Math.Max(left.Length, right.Length);
        if (max == 0)
            return 0;
        return (int)Math.Round(100.0 * (1 - Distance(left, right) / (double)max));
    }

    private static int Distance(string left, string right)
    {
        var prev = Enumerable.Range(0, right.Length + 1).ToArray();
        var curr = new int[right.Length + 1];
        for (var i = 0; i < left.Length; i++)
        {
            curr[0] = i + 1;
            for (var j = 0; j < right.Length; j++)
            {
                var cost = left[i] == right[j] ? 0 : 1;
                curr[j + 1] = Math.Min(Math.Min(curr[j] + 1, prev[j + 1] + 1), prev[j] + cost);
            }

            (prev, curr) = (curr, prev);
        }

        return prev[right.Length];
    }
}
