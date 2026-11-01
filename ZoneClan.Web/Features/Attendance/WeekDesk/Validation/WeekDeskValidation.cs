namespace ZoneClan.Web.Features.Attendance.WeekDesk.Validation;

public static class WeekDeskValidation
{
    public static bool IsValidDate(DateOnly date) => date != default;
    public static bool IsValidStage(int stage) => stage is 1 or 2 or 3;
}
