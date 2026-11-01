using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Attendance.PlayerStats.Models;
using ZoneClan.Core.Features.Attendance.Shared;

namespace ZoneClan.Server.Features.Attendance.PlayerStats.Data;

public class PlayerCardStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<PlayerCardDto?> GetAsync(ulong? playerId, string? nickname, DateOnly from, DateOnly to)
    {
        await using var connection = new MySqlConnection(_connectionString);
        string? resolvedName = nickname;
        if (playerId is > 0 && string.IsNullOrWhiteSpace(resolvedName))
        {
            resolvedName = await connection.ExecuteScalarAsync<string?>(
                "SELECT ingame_name FROM players WHERE discord_id = @Id",
                new { Id = playerId });
        }

        var lines = (await connection.QueryAsync<LineRow>(
            """
            SELECT p.tab_id AS TabId, t.week_start AS WeekStart, t.stage AS Stage,
                   p.kills AS Kills, p.deaths AS Deaths, p.assists AS Assists, p.score AS Score,
                   p.is_excused AS IsExcused, t.team1_name AS Team1Name, t.team1_score AS Team1Score,
                   t.team2_name AS Team2Name, t.team2_score AS Team2Score, t.is_valid AS IsValid
            FROM match_tab_players p
            JOIN match_tabs t ON t.id = p.tab_id
            WHERE t.week_start BETWEEN @From AND @To
              AND ((@PlayerId IS NOT NULL AND p.player_id = @PlayerId) OR (@Nick IS NOT NULL AND p.nickname = @Nick))
            ORDER BY t.week_start, t.stage
            """,
            new
            {
                PlayerId = playerId is > 0 ? playerId : null,
                Nick = string.IsNullOrWhiteSpace(nickname) ? null : nickname,
                From = from.ToDateTime(TimeOnly.MinValue),
                To = to.ToDateTime(TimeOnly.MinValue)
            })).ToList();

        if (playerId is not > 0 && string.IsNullOrWhiteSpace(nickname))
            return null;
        if (lines.Count == 0 && string.IsNullOrWhiteSpace(resolvedName))
            return null;

        return new PlayerCardDto
        {
            PlayerId = playerId is > 0 ? playerId : lines.FirstOrDefault()?.PlayerId,
            Nickname = resolvedName ?? lines.FirstOrDefault()?.Nickname ?? nickname ?? "",
            Lines = lines.Select(line => new PlayerTabLineDto
            {
                TabId = line.TabId,
                WeekStart = DateOnly.FromDateTime(line.WeekStart),
                Stage = line.Stage,
                Kills = line.Kills,
                Deaths = line.Deaths,
                Assists = line.Assists,
                Score = line.Score,
                IsExcused = line.IsExcused,
                IsWinner = line.IsValid && ClanMatch.Evaluate(line.Team1Name, line.Team1Score, line.Team2Name, line.Team2Score).IsWinner
            }).ToList()
        };
    }

    private sealed class LineRow
    {
        public int TabId { get; set; }
        public DateTime WeekStart { get; set; }
        public int Stage { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Score { get; set; }
        public bool IsExcused { get; set; }
        public string Team1Name { get; set; } = "";
        public int Team1Score { get; set; }
        public string Team2Name { get; set; } = "";
        public int Team2Score { get; set; }
        public bool IsValid { get; set; }
        public ulong? PlayerId { get; set; }
        public string Nickname { get; set; } = "";
    }
}
