using ZoneClan.Core.Features.Roster.Models;

namespace ZoneClan.Core.Features.Roster.BusinessLogic;

public static class ArmorMath
{
    public static decimal Health(decimal bulletResist, decimal vitality) =>
        Round((100m + bulletResist) * (1m + vitality / 100m));

    public static decimal HealPerSecond(decimal health, decimal healPercent) =>
        Round(health * healPercent / 100m);

    public static decimal Bleeding(decimal buildBleeding, decimal armorBleeding) =>
        Round(-10m + buildBleeding + armorBleeding);

    public static decimal Speed(decimal moveSpeed, decimal runSpeed) =>
        Round(moveSpeed + runSpeed);

    public static bool HasStats(RosterBuildDto build) =>
        build.EffectiveHealth != 0 || build.HealPerSec != 0 || build.Bleeding != 0 || build.Vitality != 0
        || build.HealEfficiency != 0 || build.HealthRegen != 0 || build.MoveSpeed != 0 || build.RunSpeed != 0
        || build.Endurance != 0 || build.EnduranceRegen != 0 || build.CarryWeight != 0 || build.BulletResist != 0
        || build.RuptureProtection != 0 || build.Resilience != 0;

    public static RosterBuildDto Apply(RosterBuildDto build, CatalogItemDto? armor)
    {
        decimal Mod(string code) =>
            armor?.Modifiers.Where(modifier => modifier.Code == code).Sum(modifier => modifier.Value) ?? 0;

        var result = new RosterBuildDto
        {
            BuildType = build.BuildType,
            HealPerSec = build.HealPerSec + Mod("heal_per_sec"),
            Bleeding = Bleeding(build.Bleeding, Mod("bleeding")),
            Vitality = build.Vitality + Mod("vitality"),
            HealEfficiency = build.HealEfficiency + Mod("heal_efficiency"),
            HealthRegen = build.HealthRegen + Mod("health_regen"),
            MoveSpeed = build.MoveSpeed + Mod("move_speed"),
            RunSpeed = build.RunSpeed + Mod("run_speed"),
            Endurance = build.Endurance + Mod("endurance"),
            EnduranceRegen = build.EnduranceRegen + Mod("endurance_regen"),
            CarryWeight = build.CarryWeight + Mod("carry_weight"),
            BulletResist = build.BulletResist + (armor?.BulletResist ?? 0) + Mod("bullet_resist"),
            RuptureProtection = (armor?.RuptureProtection ?? 0) + Mod("rupture_protection"),
            Resilience = build.Resilience + Mod("resilience")
        };
        result.EffectiveHealth = Health(result.BulletResist, result.Vitality);
        return result;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
