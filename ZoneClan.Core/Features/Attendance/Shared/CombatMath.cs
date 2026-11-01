namespace ZoneClan.Core.Features.Attendance.Shared;

public static class CombatMath
{
    public static double Kd(int kills, int deaths) =>
        Math.Round(deaths == 0 ? kills : (double)kills / deaths, 2);
}
