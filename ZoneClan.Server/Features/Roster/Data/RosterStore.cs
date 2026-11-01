using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Roles.Models;
using ZoneClan.Core.Features.Roster.BusinessLogic;
using ZoneClan.Core.Features.Roster.Models;

namespace ZoneClan.Server.Features.Roster.Data;

public class RosterStore(IConfiguration config)
{
    private static readonly HashSet<string> Statuses = ["pending", "in_clan", "on_leave", "excluded"];
    private static readonly string[] BuildTypes = ["fat", "speed", "combined"];
    private static readonly string[] NoteTypes = ["duel", "player", "commander", "timekeeper", "caller"];
    private static readonly string[] PlayerTables =
    [
        "attendance", "match_player_stats", "match_tab_players", "player_builds", "player_notes",
        "player_penalties", "player_snipers", "squad_members", "player_roles", "player_stage_status"
    ];

    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<IReadOnlyList<RosterPlayerDto>> ListAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = (await connection.QueryAsync<RosterPlayerDto>(
            """
            SELECT discord_id AS DiscordId, ingame_name AS Nickname, status AS Status,
                   has_sledgehammer AS HasSledgehammer, plays_sniper AS PlaysSniper
            FROM players
            WHERE ingame_name IS NOT NULL AND ingame_name <> ''
            ORDER BY ingame_name
            """)).ToList();

        var squads = (await connection.QueryAsync<(ulong PlayerId, int SquadId, string Name)>(
            """
            SELECT m.player_id AS PlayerId, m.squad_id AS SquadId, s.name AS Name
            FROM squad_members m
            JOIN squads s ON s.id = m.squad_id
            """)).ToList();
        var roles = (await connection.QueryAsync<PlayerRoleRow>(
            """
            SELECT pr.player_id AS PlayerId, r.id AS Id, r.name AS Name, r.color AS Color
            FROM player_roles pr
            JOIN roles r ON r.id = pr.role_id
            ORDER BY r.name
            """)).ToList();
        var stats = (await connection.QueryAsync<(ulong PlayerId, int Kills, int Deaths, int Assists, int Score)>(
            """
            SELECT p.player_id AS PlayerId,
                   SUM(p.kills) AS Kills, SUM(p.deaths) AS Deaths,
                   SUM(p.assists) AS Assists, SUM(p.score) AS Score
            FROM match_tab_players p
            JOIN match_tabs t ON t.id = p.tab_id
            LEFT JOIN player_stage_status s
              ON s.player_id = p.player_id AND s.week_start = t.week_start AND s.stage = t.stage
            WHERE t.is_valid = 1 AND p.player_id IS NOT NULL
              AND IFNULL(s.status, 'attended') = 'attended'
            GROUP BY p.player_id
            """)).ToList();

        var builds = (await connection.QueryAsync<PlayerBuildRow>(
            BuildSelect.Replace("SELECT ", "SELECT player_id AS PlayerId, "))).ToList();
        var favorites = (await connection.QueryAsync<(ulong PlayerId, int ArmorId, string Name)>(
            """
            SELECT b.player_id AS PlayerId, a.id AS ArmorId, a.name AS Name
            FROM player_armor_stats s
            JOIN player_builds b ON b.id = s.build_id
            JOIN armors a ON a.id = s.armor_id
            WHERE s.is_primary = 1
            """)).ToList();
        var catalog = await LoadCatalogAsync(connection);

        foreach (var player in rows)
        {
            var squad = squads.FirstOrDefault(item => item.PlayerId == player.DiscordId);
            if (squad.SquadId != 0)
            {
                player.SquadId = squad.SquadId;
                player.SquadName = squad.Name;
            }

            player.Roles = roles
                .Where(role => role.PlayerId == player.DiscordId)
                .Select(role => new RoleDto { Id = role.Id, Name = role.Name, Color = role.Color })
                .ToList();
            var combat = stats.FirstOrDefault(item => item.PlayerId == player.DiscordId);
            player.Kills = combat.Kills;
            player.Deaths = combat.Deaths;
            player.Assists = combat.Assists;
            player.Score = combat.Score;

            var favorite = favorites.FirstOrDefault(item => item.PlayerId == player.DiscordId);
            if (favorite.ArmorId == 0)
                continue;
            var armor = catalog.FirstOrDefault(item => item.Id == favorite.ArmorId);
            if (armor is null)
                continue;
            var loadouts = new[] { "fat", "speed", "combined" }
                .Select(type => builds.FirstOrDefault(build => build.PlayerId == player.DiscordId && build.BuildType == type))
                .Where(build => build is not null && ArmorMath.HasStats(build))
                .Select(build =>
                {
                    var applied = ArmorMath.Apply(build!, armor);
                    return new FavoriteLoadoutDto
                    {
                        BuildType = applied.BuildType,
                        Health = applied.EffectiveHealth,
                        HealPerSecond = ArmorMath.HealPerSecond(applied.EffectiveHealth, applied.HealPerSec),
                        Speed = ArmorMath.Speed(applied.MoveSpeed, applied.RunSpeed),
                        Weight = applied.CarryWeight,
                        EnduranceRegen = applied.EnduranceRegen,
                        Resilience = applied.Resilience
                    };
                })
                .ToList();
            player.Favorite = new FavoriteArmorDto { Name = favorite.Name, Loadouts = loadouts };
        }

        return rows;
    }

    public async Task<RosterCardDto?> GetAsync(ulong discordId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var player = await connection.QueryFirstOrDefaultAsync<RosterPlayerDto>(
            """
            SELECT discord_id AS DiscordId, ingame_name AS Nickname, status AS Status,
                   has_sledgehammer AS HasSledgehammer, plays_sniper AS PlaysSniper
            FROM players
            WHERE discord_id = @Id
            """,
            new { Id = discordId });
        if (player is null)
            return null;

        var storedNotes = (await connection.QueryAsync<RosterNoteDto>(
            """
            SELECT note_type AS NoteType, content AS Content
            FROM player_notes
            WHERE player_id = @Id
            ORDER BY note_type
            """,
            new { Id = discordId })).ToList();
        var notes = NoteTypes
            .Select(type => storedNotes.FirstOrDefault(note => note.NoteType == type) ?? new RosterNoteDto { NoteType = type })
            .ToList();

        var storedBuilds = (await connection.QueryAsync<RosterBuildDto>(
            BuildSelect + " WHERE player_id = @Id",
            new { Id = discordId })).ToList();
        var builds = BuildTypes
            .Select(type => storedBuilds.FirstOrDefault(build => build.BuildType == type) ?? new RosterBuildDto { BuildType = type })
            .ToList();

        var armors = (await connection.QueryAsync<RosterArmorDto>(
            """
            SELECT b.build_type AS BuildType, a.id AS ArmorId, a.name AS Name, s.is_primary AS IsFavorite, s.total_speed AS TotalSpeed,
                   s.effective_health AS EffectiveHealth, s.heal_per_sec AS HealPerSec, s.bleeding AS Bleeding,
                   s.vitality AS Vitality, s.heal_efficiency AS HealEfficiency, s.health_regen AS HealthRegen,
                   s.move_speed AS MoveSpeed, s.run_speed AS RunSpeed, s.endurance AS Endurance,
                   s.endurance_regen AS EnduranceRegen, s.carry_weight AS CarryWeight, s.bullet_resist AS BulletResist,
                   s.rupture_protection AS RuptureProtection, s.resilience AS Resilience
            FROM player_armor_stats s
            JOIN player_builds b ON b.id = s.build_id
            JOIN armors a ON a.id = s.armor_id
            WHERE b.player_id = @Id
            ORDER BY b.build_type, a.name
            """,
            new { Id = discordId })).ToList();

        var snipers = (await connection.QueryAsync<CatalogItemDto>(
            """
            SELECT s.id AS Id, s.name AS Name, ps.player_id IS NOT NULL AS Selected
            FROM snipers s
            LEFT JOIN player_snipers ps ON ps.sniper_id = s.id AND ps.player_id = @Id
            ORDER BY s.name
            """,
            new { Id = discordId })).ToList();

        var catalog = await LoadCatalogAsync(connection);
        var roleCatalog = (await connection.QueryAsync<RoleDto>(
            "SELECT id AS Id, name AS Name, color AS Color FROM roles ORDER BY name")).ToList();
        var playerRoles = (await connection.QueryAsync<RoleDto>(
            """
            SELECT r.id AS Id, r.name AS Name, r.color AS Color
            FROM player_roles pr
            JOIN roles r ON r.id = pr.role_id
            WHERE pr.player_id = @Id
            ORDER BY r.name
            """,
            new { Id = discordId })).ToList();

        return new RosterCardDto
        {
            DiscordId = player.DiscordId,
            Nickname = player.Nickname,
            Status = player.Status,
            HasSledgehammer = player.HasSledgehammer,
            PlaysSniper = player.PlaysSniper,
            Note = notes.FirstOrDefault(note => note.NoteType == "timekeeper")?.Content ?? "",
            DuelNote = notes.FirstOrDefault(note => note.NoteType == "duel")?.Content ?? "",
            Notes = notes,
            Roles = playerRoles,
            RoleCatalog = roleCatalog,
            Build = builds.First(build => build.BuildType == "combined"),
            Builds = builds,
            Armors = armors,
            ArmorCatalog = catalog,
            Snipers = snipers
        };
    }

    public async Task<ulong> UpdateAsync(ulong discordId, UpdateRosterRequest request)
    {
        var nickname = request.Nickname.Trim();
        if (nickname.Length == 0)
            throw new InvalidOperationException("Ник не может быть пустым");
        if (!Statuses.Contains(request.Status))
            throw new InvalidOperationException("Неизвестный статус");

        var newId = request.DiscordId == 0 ? discordId : request.DiscordId;
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM players WHERE discord_id = @Id",
                new { Id = discordId },
                transaction);
            if (exists == 0)
                return 0;

            if (newId != discordId)
            {
                var taken = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM players WHERE discord_id = @Id",
                    new { Id = newId },
                    transaction);
                if (taken > 0)
                    throw new InvalidOperationException("Этот Discord ID уже занят");

                await connection.ExecuteAsync(
                    """
                    INSERT INTO players (discord_id, ingame_name, status, has_sledgehammer, plays_sniper)
                    SELECT @NewId, ingame_name, status, has_sledgehammer, plays_sniper
                    FROM players WHERE discord_id = @OldId
                    """,
                    new { NewId = newId, OldId = discordId },
                    transaction);
                foreach (var table in PlayerTables)
                {
                    await connection.ExecuteAsync(
                        $"UPDATE {table} SET player_id = @NewId WHERE player_id = @OldId",
                        new { NewId = newId, OldId = discordId },
                        transaction);
                }

                await connection.ExecuteAsync(
                    "DELETE FROM players WHERE discord_id = @OldId",
                    new { OldId = discordId },
                    transaction);
                discordId = newId;
            }

            await connection.ExecuteAsync(
                """
                UPDATE players
                SET ingame_name = @Name, status = @Status, has_sledgehammer = @Sledge, plays_sniper = @Sniper
                WHERE discord_id = @Id
                """,
                new
                {
                    Id = discordId,
                    Name = nickname,
                    request.Status,
                    Sledge = request.HasSledgehammer,
                    Sniper = request.PlaysSniper
                },
                transaction);

            var notes = request.Notes.Where(note => NoteTypes.Contains(note.NoteType)).ToList();
            if (notes.Count == 0)
                notes.Add(new RosterNoteDto { NoteType = "timekeeper", Content = request.Note });
            foreach (var note in notes)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO player_notes (player_id, note_type, content, updated_by)
                    VALUES (@Id, @Type, @Content, 0)
                    ON DUPLICATE KEY UPDATE content = @Content, updated_at = NOW()
                    """,
                    new { Id = discordId, Type = note.NoteType, Content = note.Content.Trim() },
                    transaction);
            }

            var builds = request.Builds.Where(build => BuildTypes.Contains(build.BuildType)).ToList();
            if (builds.Count == 0)
            {
                var fallback = request.Build ?? new RosterBuildDto();
                fallback.BuildType = "combined";
                builds.Add(fallback);
            }

            foreach (var build in builds)
            {
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
                    BuildArgs(discordId, build),
                    transaction);
            }

            var armors = request.Armors
                .Where(armor => armor.ArmorId > 0 && BuildTypes.Contains(armor.BuildType))
                .GroupBy(armor => (armor.BuildType, armor.ArmorId))
                .Select(group => group.Last())
                .ToList();
            foreach (var buildType in builds.Select(build => build.BuildType).Distinct())
            {
                var buildId = await connection.ExecuteScalarAsync<int>(
                    "SELECT id FROM player_builds WHERE player_id = @Id AND build_type = @Type",
                    new { Id = discordId, Type = buildType },
                    transaction);
                var ofBuild = armors.Where(armor => armor.BuildType == buildType).ToList();
                if (ofBuild.Count == 0)
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
                        new { BId = buildId, Ids = ofBuild.Select(armor => armor.ArmorId).ToList() },
                        transaction);
                }

                foreach (var armor in ofBuild)
                {
                    armor.TotalSpeed = armor.MoveSpeed + armor.RunSpeed;
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
                            AId = armor.ArmorId,
                            EH = armor.EffectiveHealth,
                            HPS = armor.HealPerSec,
                            Bleed = armor.Bleeding,
                            Vit = armor.Vitality,
                            HE = armor.HealEfficiency,
                            HR = armor.HealthRegen,
                            MS = armor.MoveSpeed,
                            RS = armor.RunSpeed,
                            TS = armor.TotalSpeed,
                            End = armor.Endurance,
                            ER = armor.EnduranceRegen,
                            CW = armor.CarryWeight,
                            BR = armor.BulletResist,
                            RP = armor.RuptureProtection,
                            Res = armor.Resilience
                        },
                        transaction);
                }
            }

            var favoriteId = armors.FirstOrDefault(armor => armor.IsFavorite)?.ArmorId ?? 0;
            await connection.ExecuteAsync(
                """
                UPDATE player_armor_stats s
                JOIN player_builds b ON b.id = s.build_id
                SET s.is_primary = (s.armor_id = @Favorite)
                WHERE b.player_id = @Id
                """,
                new { Id = discordId, Favorite = favoriteId },
                transaction);

            await connection.ExecuteAsync(
                "DELETE FROM player_snipers WHERE player_id = @Id",
                new { Id = discordId },
                transaction);
            var sniperIds = request.PlaysSniper ? request.SniperIds.Distinct().ToList() : [];
            if (sniperIds.Count > 0)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO player_snipers (player_id, sniper_id)
                    SELECT @Id, id FROM snipers WHERE id IN @Ids
                    """,
                    new { Id = discordId, Ids = sniperIds },
                    transaction);
            }

            await connection.ExecuteAsync(
                "DELETE FROM player_roles WHERE player_id = @Id",
                new { Id = discordId },
                transaction);
            var roleIds = request.RoleIds.Where(id => id > 0).Distinct().ToList();
            if (roleIds.Count > 0)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO player_roles (player_id, role_id)
                    SELECT @Id, id FROM roles WHERE id IN @Ids
                    """,
                    new { Id = discordId, Ids = roleIds },
                    transaction);
            }

            await transaction.CommitAsync();
            return discordId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(ulong discordId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM players WHERE discord_id = @Id",
                new { Id = discordId },
                transaction);
            if (exists == 0)
                return false;

            await connection.ExecuteAsync(
                "UPDATE match_tab_players SET player_id = NULL WHERE player_id = @Id",
                new { Id = discordId },
                transaction);
            await connection.ExecuteAsync(
                "UPDATE squad_slots SET player_id = NULL WHERE player_id = @Id",
                new { Id = discordId },
                transaction);
            await connection.ExecuteAsync(
                "DELETE FROM players WHERE discord_id = @Id",
                new { Id = discordId },
                transaction);
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<List<CatalogItemDto>> LoadCatalogAsync(MySqlConnection connection)
    {
        var catalog = (await connection.QueryAsync<CatalogItemDto>(
            """
            SELECT id AS Id, name AS Name, bullet_resist AS BulletResist, rupture_protection AS RuptureProtection
            FROM armors
            ORDER BY name
            """)).ToList();
        var modifiers = (await connection.QueryAsync<(int ArmorId, string Code, decimal Value)>(
            "SELECT armor_id AS ArmorId, stat_code AS Code, value AS Value FROM armor_modifiers")).ToList();
        foreach (var item in catalog)
        {
            item.Modifiers = modifiers
                .Where(modifier => modifier.ArmorId == item.Id)
                .Select(modifier => new StatModifierDto { Code = modifier.Code, Value = modifier.Value })
                .ToList();
        }

        return catalog;
    }

    private sealed class PlayerBuildRow : RosterBuildDto
    {
        public ulong PlayerId { get; set; }
    }

    private sealed class PlayerRoleRow
    {
        public ulong PlayerId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "";
    }

    private const string BuildSelect =
        """
        SELECT build_type AS BuildType, effective_health AS EffectiveHealth, heal_per_sec AS HealPerSec, bleeding AS Bleeding,
               vitality AS Vitality, heal_efficiency AS HealEfficiency, health_regen AS HealthRegen,
               move_speed AS MoveSpeed, run_speed AS RunSpeed, endurance AS Endurance,
               endurance_regen AS EnduranceRegen, carry_weight AS CarryWeight, bullet_resist AS BulletResist,
               rupture_protection AS RuptureProtection, resilience AS Resilience
        FROM player_builds
        """;

    private static object BuildArgs(ulong id, RosterBuildDto build) => new
    {
        Id = id,
        Type = build.BuildType,
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
    };
}
