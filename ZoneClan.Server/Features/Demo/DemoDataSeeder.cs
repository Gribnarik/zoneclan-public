using System;
using System.Data;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using MySqlConnector;

namespace ZoneClan.Server.Features.Demo;

public class DemoDataSeeder
{
    private readonly string _connectionString;
    private readonly DemoModeConfig _config;
    private readonly Faker _faker;
    private readonly Random _random;

    public DemoDataSeeder(string connectionString, DemoModeConfig config)
    {
        _connectionString = connectionString;
        _config = config;
        _faker = new Faker("en");
        _random = new Random();
    }

    public async Task SeedAsync()
    {
        Console.WriteLine("🚀 Starting demo data seeding...");

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        try
        {
            // Clear existing data if needed
            await ClearExistingData(connection);

            // Seed core data
            await SeedPlayersAsync(connection);
            await SeedMatchTabsAsync(connection);
            await SeedMatchTabPlayersAsync(connection);
            await SeedSquadsAsync(connection);
            await SeedPlayerBuildsAsync(connection);
            await SeedPenaltiesAsync(connection);
            await SeedPlayerRolesAsync(connection);

            Console.WriteLine("✅ Demo data seeding completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding demo data: {ex.Message}");
            throw;
        }
    }

    private async Task ClearExistingData(MySqlConnection connection)
    {
        // We'll keep the schema, just clear player-related data
        Console.WriteLine("  Clearing existing player data...");

        var tables = new[]
        {
            "player_penalties", "player_notes", "player_snipers", "player_roles",
            "player_armor_stats", "player_builds", "player_stage_status",
            "match_tab_players", "squad_members", "attendance", "player_bans",
            "match_tabs", "players"
        };

        await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 0");

        foreach (var table in tables)
        {
            try
            {
                await connection.ExecuteAsync($"DELETE FROM {table}");
                Console.WriteLine($"    Cleared {table}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ⚠️  Could not clear {table}: {ex.Message}");
            }
        }

        await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 1");
    }

    private async Task SeedPlayersAsync(MySqlConnection connection)
    {
        Console.WriteLine($"  Seeding {_config.PlayerCount} players...");

        for (int i = 1; i <= _config.PlayerCount; i++)
        {
            var discordId = GenerateDiscordSnowflake();
            var nickname = GenerateGamingNickname();

            await connection.ExecuteAsync(@"
                INSERT INTO players (discord_id, ingame_name, created_at, updated_at)
                VALUES (@DiscordId, @Nickname, NOW(), NOW())",
                new { DiscordId = discordId, Nickname = nickname });

            if (i % 10 == 0)
                Console.WriteLine($"    Seeded {i} players...");
        }
    }

    private async Task SeedMatchTabsAsync(MySqlConnection connection)
    {
        Console.WriteLine($"  Seeding {_config.MatchTabCount} match tabs...");

        // Generate matches over the last 10 weeks (2 per week)
        var startDate = DateTime.UtcNow.AddDays(-70); // 10 weeks ago
        var matchesPerWeek = _config.MatchTabCount / 10;

        for (int week = 0; week < 10; week++)
        {
            for (int match = 0; match < matchesPerWeek; match++)
            {
                var matchDate = startDate.AddDays(week * 7).AddHours(_random.Next(18, 22)); // Evening matches
                var stageNumber = _random.Next(1, 6);

                await connection.ExecuteAsync(@"
                    INSERT INTO match_tabs (stage_number, match_date, created_at, updated_at)
                    VALUES (@StageNumber, @MatchDate, NOW(), NOW())",
                    new { StageNumber = stageNumber, MatchDate = matchDate });

                // Update progress
                var total = week * matchesPerWeek + match + 1;
                if (total % 5 == 0)
                    Console.WriteLine($"    Seeded {total} matches...");
            }
        }
    }

    private async Task SeedMatchTabPlayersAsync(MySqlConnection connection)
    {
        Console.WriteLine("  Seeding match tab players with statistics...");

        // Get all match tabs and players
        var matchTabs = await connection.QueryAsync<dynamic>("SELECT id FROM match_tabs");
        var players = await connection.QueryAsync<dynamic>("SELECT discord_id FROM players");

        foreach (var matchTab in matchTabs)
        {
            // 12-20 players per match
            var playersInMatch = _random.Next(12, 21);

            for (int i = 0; i < playersInMatch; i++)
            {
                var player = players.ElementAt(_random.Next(players.Count()));
                var kills = _random.Next(0, 26); // 0-25 kills
                var deaths = _random.Next(0, 21); // 0-20 deaths
                var assists = _random.Next(0, 16); // 0-15 assists

                await connection.ExecuteAsync(@"
                    INSERT INTO match_tab_players
                    (match_tab_id, player_id, nickname, kills, deaths, assists, created_at, updated_at)
                    VALUES (@MatchTabId, @PlayerId, @Nickname, @Kills, @Deaths, @Assists, NOW(), NOW())",
                    new
                    {
                        MatchTabId = matchTab.id,
                        PlayerId = player.discord_id,
                        Nickname = GenerateGamingNickname(),
                        Kills = kills,
                        Deaths = deaths,
                        Assists = assists
                    });
            }
        }

        Console.WriteLine($"    Seeded statistics for {matchTabs.Count()} matches");
    }

    private async Task SeedSquadsAsync(MySqlConnection connection)
    {
        Console.WriteLine("  Seeding squads and squad members...");

        // Create 2-3 squads
        var squadCount = _random.Next(2, 4);

        for (int i = 1; i <= squadCount; i++)
        {
            var squadId = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO squads (name, created_at, updated_at)
                VALUES (@Name, NOW(), NOW());
                SELECT LAST_INSERT_ID()",
                new { Name = $"Squad {i}" });

            // Add 4-8 players to each squad
            var players = await connection.QueryAsync<dynamic>(
                "SELECT discord_id FROM players ORDER BY RAND() LIMIT " + _random.Next(4, 9));

            foreach (var player in players)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO squad_members (squad_id, player_id, created_at, updated_at)
                    VALUES (@SquadId, @PlayerId, NOW(), NOW())",
                    new { SquadId = squadId, PlayerId = player.discord_id });
            }

            Console.WriteLine($"    Created squad '{i}' with {players.Count()} members");
        }
    }

    private async Task SeedPlayerBuildsAsync(MySqlConnection connection)
    {
        Console.WriteLine("  Seeding player builds...");

        var players = await connection.QueryAsync<dynamic>("SELECT discord_id FROM players");

        // 30-40 builds total
        var buildCount = _random.Next(30, 41);

        for (int i = 0; i < buildCount; i++)
        {
            var player = players.ElementAt(_random.Next(players.Count()));
            var buildName = _faker.PickRandom(new[] { "PVP", "PVE", "Healer", "Tank", "DPS", "Support", "Hybrid" });

            await connection.ExecuteAsync(@"
                INSERT INTO player_builds (player_id, build_name, created_at, updated_at)
                VALUES (@PlayerId, @BuildName, NOW(), NOW())",
                new { PlayerId = player.discord_id, BuildName = $"{buildName} Build" });

            // Also add armor stats for some builds
            if (_random.NextDouble() > 0.5) // 50% chance
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO player_armor_stats (player_id, armor_type, armor_value, created_at, updated_at)
                    VALUES (@PlayerId, @ArmorType, @ArmorValue, NOW(), NOW())",
                    new
                    {
                        PlayerId = player.discord_id,
                        ArmorType = _faker.PickRandom(new[] { "Light", "Medium", "Heavy" }),
                        ArmorValue = _random.Next(100, 1000)
                    });
            }
        }

        Console.WriteLine($"    Created {buildCount} player builds");
    }

    private async Task SeedPenaltiesAsync(MySqlConnection connection)
    {
        Console.WriteLine("  Seeding player penalties...");

        var players = await connection.QueryAsync<dynamic>("SELECT discord_id FROM players");
        var penaltyTypes = new[] { "Late", "No Show", "Bad Behavior", "AFK", "Early Leave" };

        // 10-15 penalties
        var penaltyCount = _random.Next(10, 16);

        for (int i = 0; i < penaltyCount; i++)
        {
            var player = players.ElementAt(_random.Next(players.Count()));
            var penaltyType = penaltyTypes[_random.Next(penaltyTypes.Length)];
            var severity = _random.Next(1, 6); // 1-5 severity
            var daysAgo = _random.Next(1, 91); // Last 90 days
            var penaltyDate = DateTime.UtcNow.AddDays(-daysAgo);

            await connection.ExecuteAsync(@"
                INSERT INTO player_penalties (player_id, penalty_type, severity, penalty_date, notes, created_at, updated_at)
                VALUES (@PlayerId, @PenaltyType, @Severity, @PenaltyDate, @Notes, NOW(), NOW())",
                new
                {
                    PlayerId = player.discord_id,
                    PenaltyType = penaltyType,
                    Severity = severity,
                    PenaltyDate = penaltyDate,
                    Notes = _faker.Lorem.Sentence()
                });
        }

        Console.WriteLine($"    Created {penaltyCount} penalties");
    }

    private async Task SeedPlayerRolesAsync(MySqlConnection connection)
    {
        Console.WriteLine("  Seeding player roles...");

        var players = await connection.QueryAsync<dynamic>("SELECT discord_id FROM players");
        var roles = new[] { "Commander", "Builder", "Medic", "Scout", "Engineer", "Sniper", "Support" };

        // 15-20 role assignments
        var roleCount = _random.Next(15, 21);

        for (int i = 0; i < roleCount; i++)
        {
            var player = players.ElementAt(_random.Next(players.Count()));
            var role = roles[_random.Next(roles.Length)];

            await connection.ExecuteAsync(@"
                INSERT INTO player_roles (player_id, role_name, created_at, updated_at)
                VALUES (@PlayerId, @RoleName, NOW(), NOW())",
                new { PlayerId = player.discord_id, RoleName = role });
        }

        Console.WriteLine($"    Assigned {roleCount} player roles");
    }

    private long GenerateDiscordSnowflake()
    {
        // Generate fake Discord ID in the range 1100000000000000000-1199999999999999999
        var min = 1100000000000000000L;
        var max = 1199999999999999999L;

        var bytes = new byte[8];
        _random.NextBytes(bytes);
        var value = BitConverter.ToUInt64(bytes, 0);

        // Cast max-min+1 to long to avoid ulong arithmetic issues
        var range = (long)(max - min + 1);
        return min + (long)(value % (ulong)range);
    }

    private string GenerateGamingNickname()
    {
        var patterns = new Func<string>[]
        {
            () => $"{_faker.Hacker.Adjective()}{_faker.Hacker.Noun()}",
            () => $"{_faker.Commerce.ProductAdjective()}{_faker.Commerce.ProductMaterial()}",
            () => $"{_faker.Name.FirstName()}{_random.Next(1, 999)}",
            () => $"{_faker.Internet.UserName()}{_random.Next(10, 99)}",
            () => $"xX_{_faker.Internet.UserName()}_Xx",
            () => $"{_faker.Hacker.Verb()}{_faker.Hacker.Noun()}",
            () => $"{_faker.Address.CityPrefix()}{_faker.Address.CitySuffix()}"
        };

        var pattern = patterns[_random.Next(patterns.Length)];
        var nickname = pattern();

        return nickname.Length > 64 ? nickname.Substring(0, 64) : nickname;
    }
}