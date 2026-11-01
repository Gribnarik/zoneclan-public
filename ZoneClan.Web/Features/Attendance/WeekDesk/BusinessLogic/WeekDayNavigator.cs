namespace ZoneClan.Web.Features.Attendance.WeekDesk.BusinessLogic;

public static class WeekDayNavigator
{
    public static DateOnly Snap(DateOnly date)
    {
        var d = date;
        while (d.DayOfWeek is not (DayOfWeek.Thursday or DayOfWeek.Friday or DayOfWeek.Saturday))
            d = d.AddDays(1);
        return d;
    }
    public static DateOnly Shift(DateOnly current, int days)
    {
        var next = current.AddDays(days);
        while (next.DayOfWeek is not (DayOfWeek.Thursday or DayOfWeek.Friday or DayOfWeek.Saturday))
            next = next.AddDays(days > 0 ? 1 : -1);
        return next;
    }
    public static string Name(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Thursday => "Чт",
        DayOfWeek.Friday => "Пт",
        DayOfWeek.Saturday => "Сб",
        _ => date.DayOfWeek.ToString()
    };
}
