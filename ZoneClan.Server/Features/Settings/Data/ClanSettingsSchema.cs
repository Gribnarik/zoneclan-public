using MySqlConnector;

namespace ZoneClan.Server.Features.Settings.Data;

public static class ClanSettingsSchema
{
    public static async Task EnsureCreatedAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS clan_settings (
              guild_id BIGINT NOT NULL PRIMARY KEY,
              clan_name VARCHAR(64) NOT NULL DEFAULT '',
              clan_tag VARCHAR(16) NOT NULL DEFAULT '',
              clan_description TEXT,
              updated_by BIGINT NOT NULL DEFAULT 0,
              updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            )
            """);

        string[] columns =
        [
            "clan_role_id",
            "rank_new_role_id",
            "rank_mid_role_id",
            "rank_veteran_role_id",
            "recruiter_role_id",
            "duelist_role_id",
            "caller_role_id",
            "timekeeper_role_id"
        ];
        foreach (var column in columns)
            await AddColumnIfMissingAsync(connection, column);
    }

    private static async Task AddColumnIfMissingAsync(MySqlConnection connection, string column)
    {
        try
        {
            await ExecuteAsync(connection, $"ALTER TABLE clan_settings ADD COLUMN {column} BIGINT NULL");
        }
        catch (MySqlException ex) when (ex.Number is 1060)
        {
            // колонка уже есть
        }
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
