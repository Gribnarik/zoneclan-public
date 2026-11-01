namespace ZoneClan.Core.Features.Tickets.Models;

public class PlayerStatsDto
{
    public decimal EffectiveHealth { get; set; }
    public decimal HealPerSec { get; set; }
    public decimal Bleeding { get; set; }
    public decimal Vitality { get; set; }
    public decimal HealEfficiency { get; set; }
    public decimal HealthRegen { get; set; }
    public decimal MoveSpeed { get; set; }
    public decimal RunSpeed { get; set; }
    public decimal Endurance { get; set; }
    public decimal EnduranceRegen { get; set; }
    public decimal CarryWeight { get; set; }
    public decimal BulletResist { get; set; }
    public decimal Resilience { get; set; }
}
