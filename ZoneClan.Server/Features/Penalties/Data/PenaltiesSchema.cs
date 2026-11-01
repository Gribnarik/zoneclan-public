using Dapper;
using MySqlConnector;
using ZoneClan.Server.Features.Shared;

namespace ZoneClan.Server.Features.Penalties.Data;

public static class PenaltiesSchema
{
    public static async Task EnsureCreatedAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await SchemaSql.AddColumnAsync(connection,
            "ALTER TABLE player_penalties ADD COLUMN resolution_method VARCHAR(32) NULL");

        var seeds = new (string Name, int Points)[]
        {
            ("Предупреждение", 0),
            ("Опоздание без отписки", 1),
            ("Пропуск этапа КВ", 2),
            ("Грубое нарушение (Командир)", 3),
            ("Грубое нарушение (Коллер)", 3)
        };
        foreach (var seed in seeds)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO penalty_types (name, default_points)
                SELECT @Name, @Points FROM DUAL
                WHERE NOT EXISTS (SELECT 1 FROM penalty_types WHERE name = @Name)
                """,
                new { seed.Name, seed.Points });
        }
    }
}
