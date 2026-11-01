using Dapper;
using MySqlConnector;

namespace ZoneClan.Server.Features.Attendance.UploadTab.Data;

public class KnownPlayer
{
    public ulong DiscordId { get; set; }
    public string Nickname { get; set; } = "";
    public string Status { get; set; } = "";
    public int Characteristic { get; set; }
}

public class AttendanceReadStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<IReadOnlyList<KnownPlayer>> GetPlayersAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<KnownPlayer>(
            """
            SELECT discord_id AS DiscordId, ingame_name AS Nickname, IFNULL(status, '') AS Status, characteristic AS Characteristic
            FROM players
            WHERE ingame_name IS NOT NULL AND ingame_name <> ''
            """);
        return rows.ToList();
    }

    public async Task<bool> TabExistsAsync(DateOnly weekStart, int stage)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM match_tabs WHERE week_start = @Week AND stage = @Stage",
            new { Week = weekStart.ToDateTime(TimeOnly.MinValue), Stage = stage });
        return count > 0;
    }

    public async Task UpdateCharacteristicAsync(ulong playerId, int value)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "UPDATE players SET characteristic = @Value WHERE discord_id = @Id",
            new { Id = playerId, Value = Math.Clamp(value, 0, 40) });
    }
}

