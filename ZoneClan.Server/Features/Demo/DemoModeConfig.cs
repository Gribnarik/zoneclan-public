namespace ZoneClan.Server.Features.Demo;

public class DemoModeConfig
{
    public bool Enabled { get; set; }
    public bool SeedData { get; set; }
    public int PlayerCount { get; set; } = 50;
    public int MatchTabCount { get; set; } = 20;
}