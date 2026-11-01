using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Bot.Features.Reminders;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

var botToken = builder.Configuration["Discord:Token"];
if (string.IsNullOrWhiteSpace(botToken))
{
    throw new InvalidOperationException(
        "Discord bot token not configured. " +
        "Copy appsettings.Local.json.template to appsettings.Local.json " +
        "and add your Discord bot token from https://discord.com/developers/applications");
}

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5203";

builder.Services.AddHostedService<ReminderService>();
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

builder.Services
    .AddDiscordGateway(options => { options.Token = botToken; })
    .AddApplicationCommands()
    .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
    .AddComponentInteractions<UserMenuInteraction, UserMenuInteractionContext>()
    .AddComponentInteractions<RoleMenuInteraction, RoleMenuInteractionContext>()
    .AddComponentInteractions<ChannelMenuInteraction, ChannelMenuInteractionContext>()
    .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>()
    .AddComponentInteractions<ModalInteraction, ModalInteractionContext>();

var host = builder.Build();
host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
