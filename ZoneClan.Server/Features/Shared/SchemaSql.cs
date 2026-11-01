using MySqlConnector;

namespace ZoneClan.Server.Features.Shared;

public static class SchemaSql
{
    public static async Task ExecuteAsync(MySqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public static async Task AddColumnAsync(MySqlConnection connection, string sql)
    {
        try
        {
            await ExecuteAsync(connection, sql);
        }
        catch (MySqlException ex) when (ex.Number is 1060)
        {
        }
    }
}
