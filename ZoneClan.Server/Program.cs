using ZoneClan.Server.Features;
using ZoneClan.Server.Features.Attendance.Data;
using ZoneClan.Server.Features.Penalties.Data;
using ZoneClan.Server.Features.Settings.Data;
using ZoneClan.Server.Features.Squads.Data;
using ZoneClan.Server.Features.Demo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddZoneClanFeatures();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Не задана ConnectionStrings:DefaultConnection.");
await ClanSettingsSchema.EnsureCreatedAsync(connectionString);
await AttendanceSchema.EnsureCreatedAsync(connectionString);
await SquadsSchema.EnsureCreatedAsync(connectionString);
await PenaltiesSchema.EnsureCreatedAsync(connectionString);

// Demo mode seeding
var demoConfig = builder.Configuration
    .GetSection("DemoMode")
    .Get<DemoModeConfig>();

if (demoConfig?.Enabled == true && demoConfig.SeedData)
{
    var seeder = new DemoDataSeeder(connectionString, demoConfig);
    await seeder.SeedAsync();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
app.Run();
