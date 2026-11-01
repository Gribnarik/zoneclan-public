using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using MySqlConnector;

namespace AnonymizationTool
{
    public class DiscordIdMapping
    {
        public long OldId { get; set; }
        public long NewId { get; set; }
        public string NewNickname { get; set; } = string.Empty;
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== ZoneClan Database Anonymization Tool ===");
            Console.WriteLine();

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet run -- <connection_string> [--dry-run]");
                Console.WriteLine("Example: dotnet run -- \"Server=localhost;Database=clan_hub;User=root;Password=yourpass\" --dry-run");
                return;
            }

            var connectionString = args[0];
            var dryRun = args.Contains("--dry-run");

            try
            {
                if (dryRun)
                {
                    Console.WriteLine("⚠️  DRY RUN MODE: No changes will be made to the database");
                    Console.WriteLine();
                }

                Console.WriteLine($"Connecting to database...");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if database has data
                var playerCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM players");
                Console.WriteLine($"Found {playerCount} players in database.");

                if (playerCount == 0)
                {
                    Console.WriteLine("⚠️  Database appears to be empty or tables don't exist.");
                    return;
                }

                // Get all existing Discord IDs
                var discordIds = (await connection.QueryAsync<long>("SELECT discord_id FROM players")).ToList();
                Console.WriteLine($"Extracted {discordIds.Count} Discord IDs to anonymize.");

                // Generate mapping
                var mapping = GenerateMapping(discordIds);
                Console.WriteLine($"Generated {mapping.Count} mappings with new Discord IDs and nicknames.");

                if (dryRun)
                {
                    Console.WriteLine("\n📋 Generated mappings (first 5):");
                    foreach (var map in mapping.Take(5))
                    {
                        Console.WriteLine($"  {map.OldId} → {map.NewId} ({map.NewNickname})");
                    }

                    Console.WriteLine("\n✅ Dry run completed. No changes made.");
                    Console.WriteLine("\nTo apply changes, run without --dry-run flag.");
                    return;
                }

                // Apply anonymization
                Console.WriteLine("\n🚀 Starting anonymization...");
                await AnonymizeDatabase(connection, mapping);

                Console.WriteLine("\n✅ Anonymization completed successfully!");
                Console.WriteLine($"\nSummary:");
                Console.WriteLine($"  - Players anonymized: {mapping.Count}");
                Console.WriteLine($"  - New Discord IDs generated in range: 1100000000000000000-1199999999999999999");
                Console.WriteLine($"  - Player nicknames replaced with gaming-style names");

                // Save mapping to CSV for reference
                SaveMappingToCsv(mapping);
                Console.WriteLine($"  - Mapping saved to: discord_id_mapping.csv");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static List<DiscordIdMapping> GenerateMapping(List<long> discordIds)
        {
            var faker = new Faker("en");
            var random = new Random();
            var mapping = new List<DiscordIdMapping>();

            foreach (var oldId in discordIds)
            {
                var newId = GenerateDiscordSnowflake(random);
                var nickname = GenerateGamingNickname(faker);

                mapping.Add(new DiscordIdMapping
                {
                    OldId = oldId,
                    NewId = newId,
                    NewNickname = nickname
                });
            }

            return mapping;
        }

        static long GenerateDiscordSnowflake(Random random)
        {
            // Discord snowflakes are 64-bit integers
            // Generate in range 1100000000000000000-1199999999999999999
            // to avoid collisions with real Discord IDs
            var min = 1100000000000000000L;
            var max = 1199999999999999999L;

            // Generate random 64-bit number in range
            var bytes = new byte[8];
            random.NextBytes(bytes);
            var value = BitConverter.ToUInt64(bytes, 0);

            // Scale to desired range
            var scaled = (long)(min + (value % (ulong)(max - min + 1)));

            return scaled;
        }

        static string GenerateGamingNickname(Faker faker)
        {
            // Gaming nickname patterns
            var patterns = new[]
            {
                () => $"{faker.Hacker.Adjective()}{faker.Hacker.Noun()}",
                () => $"{faker.Commerce.ProductAdjective()}{faker.Commerce.ProductMaterial()}",
                () => $"{faker.Name.FirstName()}{faker.Random.Number(1, 999)}",
                () => $"{faker.Company.BsNoun()}{faker.Random.Number(10, 99)}",
                () => $"xX_{faker.Internet.UserName()}_Xx",
                () => $"{faker.Hacker.Verb()}{faker.Hacker.Noun()}",
                () => $"{faker.Address.CityPrefix()}{faker.Address.CitySuffix()}",
                () => $"{faker.Company.CatchPhraseAdjective()}{faker.Company.CatchPhraseNoun()}"
            };

            var pattern = faker.PickRandom(patterns);
            var nickname = pattern();

            // Ensure nickname length is reasonable
            if (nickname.Length > 64)
                nickname = nickname.Substring(0, 64);

            return nickname;
        }

        static async Task AnonymizeDatabase(MySqlConnection connection, List<DiscordIdMapping> mapping)
        {
            // Create temporary mapping table
            Console.WriteLine("  Creating temporary mapping table...");
            await connection.ExecuteAsync(@"
                CREATE TEMPORARY TABLE discord_id_mapping (
                    old_id BIGINT PRIMARY KEY,
                    new_id BIGINT NOT NULL UNIQUE,
                    new_nickname VARCHAR(64) NOT NULL
                )");

            // Insert mappings
            Console.WriteLine($"  Inserting {mapping.Count} mappings...");
            foreach (var map in mapping)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO discord_id_mapping (old_id, new_id, new_nickname) VALUES (@OldId, @NewId, @NewNickname)",
                    map);
            }

            // Disable foreign key checks
            Console.WriteLine("  Disabling foreign key checks...");
            await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 0");

            try
            {
                // Update players table
                Console.WriteLine("  Updating players table...");
                var updatedPlayers = await connection.ExecuteAsync(@"
                    UPDATE players p
                    JOIN discord_id_mapping m ON p.discord_id = m.old_id
                    SET p.discord_id = m.new_id, p.ingame_name = m.new_nickname");
                Console.WriteLine($"    Updated {updatedPlayers} players");

                // Update match_tab_players table
                Console.WriteLine("  Updating match_tab_players table...");
                var updatedMatchPlayers = await connection.ExecuteAsync(@"
                    UPDATE match_tab_players mtp
                    JOIN discord_id_mapping m ON mtp.player_id = m.old_id
                    SET mtp.player_id = m.new_id, mtp.nickname = m.new_nickname");
                Console.WriteLine($"    Updated {updatedMatchPlayers} match player records");

                // Update other tables with player_id foreign keys
                var tablesWithPlayerId = new[]
                {
                    "player_stage_status",
                    "player_builds",
                    "player_armor_stats",
                    "player_roles",
                    "player_penalties",
                    "player_snipers",
                    "player_notes",
                    "squad_members",
                    "attendance"
                };

                foreach (var table in tablesWithPlayerId)
                {
                    try
                    {
                        var updated = await connection.ExecuteAsync($@"
                            UPDATE {table} t
                            JOIN discord_id_mapping m ON t.player_id = m.old_id
                            SET t.player_id = m.new_id");
                        Console.WriteLine($"    Updated {updated} records in {table}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    ⚠️  Could not update {table}: {ex.Message}");
                    }
                }

                // Anonymize guild_id in clan_settings
                Console.WriteLine("  Anonymizing clan_settings...");
                await connection.ExecuteAsync(@"
                    UPDATE clan_settings
                    SET guild_id = 1000000000000000000, updated_by = 0");

                // Update any other tables that reference discord_id directly
                Console.WriteLine("  Checking for other discord_id references...");
                await connection.ExecuteAsync(@"
                    UPDATE player_bans
                    JOIN discord_id_mapping m ON player_bans.player_id = m.old_id
                    SET player_bans.player_id = m.new_id");

            }
            finally
            {
                // Re-enable foreign key checks
                Console.WriteLine("  Re-enabling foreign key checks...");
                await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 1");

                // Drop temporary table
                Console.WriteLine("  Cleaning up temporary table...");
                await connection.ExecuteAsync("DROP TEMPORARY TABLE discord_id_mapping");
            }
        }

        static void SaveMappingToCsv(List<DiscordIdMapping> mapping)
        {
            try
            {
                using var writer = new StreamWriter("discord_id_mapping.csv");
                writer.WriteLine("old_id,new_id,new_nickname");

                foreach (var map in mapping)
                {
                    writer.WriteLine($"{map.OldId},{map.NewId},{map.NewNickname}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not save mapping CSV: {ex.Message}");
            }
        }
    }
}