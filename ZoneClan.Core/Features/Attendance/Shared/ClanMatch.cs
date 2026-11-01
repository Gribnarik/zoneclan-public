namespace ZoneClan.Core.Features.Attendance.Shared;

public static class ClanMatch
{
    public const string ClanName = "Lumos";

    public static bool IsTabDay(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Thursday or DayOfWeek.Friday or DayOfWeek.Saturday;

    public static DateOnly ThursdayOf(DateOnly date)
    {
        while (date.DayOfWeek != DayOfWeek.Thursday)
            date = date.AddDays(-1);
        return date;
    }

    public static DateOnly StageDate(DateOnly thursday, int stage) =>
        thursday.AddDays(Math.Clamp(stage, 1, 3) - 1);

    public static int StageOf(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Thursday => 1,
        DayOfWeek.Friday => 2,
        DayOfWeek.Saturday => 3,
        _ => 0
    };

    public static (bool Recognized, bool IsWinner) Evaluate(string? team1Name, int team1Score, string? team2Name, int team2Score)
    {
        var team1Ours = ContainsClan(team1Name);
        var team2Ours = ContainsClan(team2Name);
        var recognized = team1Ours || team2Ours;
        var isWinner = recognized && (team1Ours ? team1Score > team2Score : team2Score > team1Score);
        return (recognized, isWinner);
    }

    public static bool ContainsClan(string? name) =>
        name?.Contains(ClanName, StringComparison.OrdinalIgnoreCase) == true;
}
