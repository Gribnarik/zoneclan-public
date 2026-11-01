using ZoneClan.Web;
using ZoneClan.Web.Features.Attendance.Data;
using ZoneClan.Web.Features.Attendance.WeekDesk.BusinessLogic;
using ZoneClan.Web.Features.Penalties.Data;
using ZoneClan.Web.Features.Roster.Data;
using ZoneClan.Web.Features.Roster.BusinessLogic;
using ZoneClan.Web.Features.Squads.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5203";
var apiRoot = new Uri(apiBaseUrl.TrimEnd('/') + "/");
builder.Services.AddHttpClient<AttendanceApi>(client => client.BaseAddress = apiRoot);
builder.Services.AddScoped<WeekDeskService>();
builder.Services.AddHttpClient<RosterApi>(client => client.BaseAddress = apiRoot);
builder.Services.AddScoped<RosterService>();
builder.Services.AddHttpClient<SquadsApi>(client => client.BaseAddress = apiRoot);
builder.Services.AddHttpClient<PenaltiesApi>(client => client.BaseAddress = apiRoot);

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
