using ZoneClan.Server.Features.Armors.List.Data;
using ZoneClan.Server.Features.Attendance.ClanStats.Data;
using ZoneClan.Server.Features.Attendance.Excuses.Data;
using ZoneClan.Server.Features.Attendance.PlayerStats.Data;
using ZoneClan.Server.Features.Attendance.TabsList.Data;
using ZoneClan.Server.Features.Attendance.UploadTab.BusinessLogic;
using ZoneClan.Server.Features.Attendance.UploadTab.Data;
using ZoneClan.Server.Features.Penalties.Data;
using ZoneClan.Server.Features.Roles.Data; // <-- ДОБАВЛЕНО
using ZoneClan.Server.Features.Roster.Data;
using ZoneClan.Server.Features.Settings.Data;
using ZoneClan.Server.Features.Squads.Data;
using ZoneClan.Server.Features.Tickets.Create.BusinessLogic;

namespace ZoneClan.Server.Features;

public static class FeatureRegistration
{
    public static IServiceCollection AddZoneClanFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateTicketHandler>();
        services.AddScoped<GetArmorsQuery>();
        services.AddScoped<ClanSettingsStore>();
        services.AddSingleton<AttendanceDraftStore>();
        services.AddScoped<OcrService>();
        services.AddScoped<AttendanceReadStore>();
        services.AddScoped<PreviewTabHandler>();
        services.AddScoped<SaveTabCommand>();
        services.AddScoped<SaveTabHandler>();
        services.AddScoped<UpdateTabCommand>();
        services.AddScoped<UpdateTabHandler>();
        services.AddScoped<WeekDeskStore>();
        services.AddScoped<PlayerCardStore>();
        services.AddScoped<AttendanceStatsStore>();
        services.AddScoped<ExcuseStore>();
        services.AddScoped<RosterStore>();
        services.AddScoped<SquadsStore>();
        services.AddScoped<PenaltiesStore>();
        services.AddScoped<RolesStore>(); 
        
        return services;
    }
}