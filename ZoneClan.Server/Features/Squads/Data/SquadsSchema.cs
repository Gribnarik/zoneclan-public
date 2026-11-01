using Dapper;
using MySqlConnector;

namespace ZoneClan.Server.Features.Squads.Data;

public static class SquadsSchema
{
    public static async Task EnsureCreatedAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM squads");
        if (count > 0)
            return;

        await connection.ExecuteAsync(
            """
            INSERT INTO squads (name, description) VALUES
            ('Отряд 1', ''),
            ('Отряд 2', '')
            """);
    }
}
