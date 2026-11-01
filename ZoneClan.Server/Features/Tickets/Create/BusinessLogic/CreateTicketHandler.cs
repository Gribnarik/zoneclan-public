using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Roster.BusinessLogic;
using ZoneClan.Core.Features.Roster.Models;
using ZoneClan.Core.Features.Tickets.BusinessLogic;
using ZoneClan.Core.Features.Tickets.Models;
using ZoneClan.Server.Features.Tickets.Create.Models;

namespace ZoneClan.Server.Features.Tickets.Create.BusinessLogic;

public class CreateTicketHandler(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task ExecuteAsync(TicketRequest request)
    {
        var builds = request.Builds.Count > 0
            ? request.Builds
            :
            [
                new TicketBuildRequest
                {
                    BuildType = request.BuildType,
                    RawStatsText = request.RawStatsText,
                    ArmorIds = request.ArmorIds
                }
            ];

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var status = request.Status is "in_clan" or "on_leave" or "excluded" ? request.Status : "pending";
            await connection.ExecuteAsync(
                """
                INSERT INTO players (discord_id, ingame_name, status, has_sledgehammer, plays_sniper)
                VALUES (@Id, @Name, @Status, @Sledge, @Sniper)
                ON DUPLICATE KEY UPDATE
                    ingame_name = @Name, status = @Status, has_sledgehammer = @Sledge, plays_sniper = @Sniper
                """,
                new
                {
                    Id = request.DiscordId,
                    Name = request.IngameName,
                    Status = status,
                    Sledge = request.HasSledgehammer,
                    Sniper = request.PlaysSniper
                },
                transaction);

            if (!string.IsNullOrWhiteSpace(request.DuelNote))
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO player_notes (player_id, note_type, content, updated_by)
                    VALUES (@Id, 'duel', @Content, @UpdatedBy)
                    ON DUPLICATE KEY UPDATE content = @Content, updated_by = @UpdatedBy, updated_at = NOW()
                    """,
                    new
                    {
                        Id = request.DiscordId,
                        Content = request.DuelNote,
                        UpdatedBy = request.SubmittedBy
                    },
                    transaction);
            }

            foreach (var item in builds)
                await SaveBuildAsync(connection, transaction, request.DiscordId, item);

            if (request.FavoriteArmorId > 0)
            {
                await connection.ExecuteAsync(
                    """
                    UPDATE player_armor_stats s
                    JOIN player_builds b ON b.id = s.build_id
                    SET s.is_primary = (s.armor_id = @Favorite)
                    WHERE b.player_id = @Id
                    """,
                    new { Id = request.DiscordId, Favorite = request.FavoriteArmorId },
                    transaction);
            }

            await connection.ExecuteAsync(
                "DELETE FROM player_snipers WHERE player_id = @Id",
                new { Id = request.DiscordId },
                transaction);
            var sniperIds = request.PlaysSniper ? request.SniperIds.Distinct().ToList() : [];
            if (sniperIds.Count > 0)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO player_snipers (player_id, sniper_id)
                    SELECT @Id, id FROM snipers WHERE id IN @Ids
                    """,
                    new { Id = request.DiscordId, Ids = sniperIds },
                    transaction);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task SaveBuildAsync(
            MySqlConnection connection,
            MySqlTransaction transaction,
            ulong playerId,
            TicketBuildRequest item)
        {
            if (string.IsNullOrWhiteSpace(item.RawStatsText))
                return;

            var buildType = item.BuildType is "fat" or "speed" or "combined" ? item.BuildType : "combined";
            var parsed = StatsParser.Parse(item.RawStatsText);
            var build = new RosterBuildDto
            {
                BuildType = buildType,
                EffectiveHealth = parsed.EffectiveHealth,
                HealPerSec = parsed.HealPerSec,
                Bleeding = parsed.Bleeding,
                Vitality = parsed.Vitality,
                HealEfficiency = parsed.HealEfficiency,
                HealthRegen = parsed.HealthRegen,
                MoveSpeed = parsed.MoveSpeed,
                RunSpeed = parsed.RunSpeed,
                Endurance = parsed.Endurance,
                EnduranceRegen = parsed.EnduranceRegen,
                CarryWeight = parsed.CarryWeight,
                BulletResist = parsed.BulletResist,
                Resilience = parsed.Resilience
            };
            if (!ArmorMath.HasStats(build))
                return;

            await connection.ExecuteAsync(
                """
                INSERT INTO player_builds (
                    player_id, build_type, effective_health, heal_per_sec, bleeding, vitality, heal_efficiency,
                    health_regen, move_speed, run_speed, endurance, endurance_regen, carry_weight, bullet_resist,
                    rupture_protection, resilience)
                VALUES (
                    @Id, @Type, @EH, @HPS, @Bleed, @Vit, @HE, @HR, @MS, @RS, @End, @ER, @CW, @BR, @RP, @Res)
                ON DUPLICATE KEY UPDATE
                    effective_health = @EH, heal_per_sec = @HPS, bleeding = @Bleed, vitality = @Vit,
                    heal_efficiency = @HE, health_regen = @HR, move_speed = @MS, run_speed = @RS,
                    endurance = @End, endurance_regen = @ER, carry_weight = @CW, bullet_resist = @BR,
                    rupture_protection = @RP, resilience = @Res
                """,
                new
                {
                    Id = playerId,
                    Type = buildType,
                    EH = build.EffectiveHealth,
                    HPS = build.HealPerSec,
                    Bleed = build.Bleeding,
                    Vit = build.Vitality,
                    HE = build.HealEfficiency,
                    HR = build.HealthRegen,
                    MS = build.MoveSpeed,
                    RS = build.RunSpeed,
                    End = build.Endurance,
                    ER = build.EnduranceRegen,
                    CW = build.CarryWeight,
                    BR = build.BulletResist,
                    RP = build.RuptureProtection,
                    Res = build.Resilience
                },
                transaction);

            var buildId = await connection.ExecuteScalarAsync<int>(
                "SELECT id FROM player_builds WHERE player_id = @Id AND build_type = @Type",
                new { Id = playerId, Type = buildType },
                transaction);

            var armorIds = item.ArmorIds.Distinct().ToList();
            if (armorIds.Count == 0)
            {
                await connection.ExecuteAsync(
                    "DELETE FROM player_armor_stats WHERE build_id = @BId",
                    new { BId = buildId },
                    transaction);
            }
            else
            {
                await connection.ExecuteAsync(
                    "DELETE FROM player_armor_stats WHERE build_id = @BId AND armor_id NOT IN @Ids",
                    new { BId = buildId, Ids = armorIds },
                    transaction);
            }

            foreach (var armorId in armorIds)
                await SaveArmorStatsAsync(connection, transaction, build, buildId, armorId);
        }

    private static async Task SaveArmorStatsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        RosterBuildDto build,
        int buildId,
        int armorId)
    {
        var armor = await connection.QueryFirstOrDefaultAsync<CatalogItemDto>(
            """
            SELECT id AS Id, bullet_resist AS BulletResist, rupture_protection AS RuptureProtection
            FROM armors WHERE id = @Id
            """,
            new { Id = armorId },
            transaction);
        if (armor is null)
            return;

        armor.Modifiers = (await connection.QueryAsync<StatModifierDto>(
                "SELECT stat_code AS Code, value AS Value FROM armor_modifiers WHERE armor_id = @Id",
                new { Id = armorId },
                transaction)).ToList();
        var applied = ArmorMath.Apply(build, armor);
        var totalSpeed = ArmorMath.Speed(applied.MoveSpeed, applied.RunSpeed);

        await connection.ExecuteAsync(
            """
            INSERT INTO player_armor_stats (
                build_id, armor_id, effective_health, heal_per_sec, bleeding, vitality, heal_efficiency,
                health_regen, move_speed, run_speed, total_speed, endurance, endurance_regen, carry_weight,
                bullet_resist, rupture_protection, resilience)
            VALUES (
                @BId, @AId, @EH, @HPS, @Bleed, @Vit, @HE, @HR, @MS, @RS, @TS, @End, @ER, @CW, @BR, @RP, @Res)
            ON DUPLICATE KEY UPDATE
                effective_health = @EH, heal_per_sec = @HPS, bleeding = @Bleed, vitality = @Vit,
                heal_efficiency = @HE, health_regen = @HR, move_speed = @MS, run_speed = @RS,
                total_speed = @TS, endurance = @End, endurance_regen = @ER, carry_weight = @CW,
                bullet_resist = @BR, rupture_protection = @RP, resilience = @Res
            """,
            new
            {
                BId = buildId,
                AId = armorId,
                EH = applied.EffectiveHealth,
                HPS = applied.HealPerSec,
                Bleed = applied.Bleeding,
                Vit = applied.Vitality,
                HE = applied.HealEfficiency,
                HR = applied.HealthRegen,
                MS = applied.MoveSpeed,
                RS = applied.RunSpeed,
                TS = totalSpeed,
                End = applied.Endurance,
                ER = applied.EnduranceRegen,
                CW = applied.CarryWeight,
                BR = applied.BulletResist,
                RP = applied.RuptureProtection,
                Res = applied.Resilience
            },
            transaction);
    }
}
