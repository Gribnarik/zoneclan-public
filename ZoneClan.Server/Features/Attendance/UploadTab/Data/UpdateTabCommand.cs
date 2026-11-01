using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Attendance.UploadTab.Models;

namespace ZoneClan.Server.Features.Attendance.UploadTab.Data;

public class UpdateTabCommand(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task ExecuteAsync(int tabId, SaveTabRequest request, bool isValid, bool isWinner)
    {
        request.DayOfWeek = (int)request.WeekStart.DayOfWeek;
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            string? result = isValid ? (isWinner ? "win" : "loss") : null;

            await connection.ExecuteAsync(
                """
                UPDATE match_tabs
                SET team1_name = @Team1, team1_score = @Score1, team2_name = @Team2, team2_score = @Score2,
                    guilds = @Guilds, is_valid = @Valid, result = @Result
                WHERE id = @Id
                """,
                new
                {
                    Id = tabId,
                    Team1 = request.Team1Name,
                    Score1 = request.Team1Score,
                    Team2 = request.Team2Name,
                    Score2 = request.Team2Score,
                    request.Guilds,
                    Valid = isValid,
                    Result = result
                },
                transaction);

            await connection.ExecuteAsync("DELETE FROM match_tab_players WHERE tab_id = @Id", new { Id = tabId }, transaction);

            var sortedPlayers = request.Players.OrderByDescending(p => p.Score).ToList();
            foreach (var player in sortedPlayers)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO match_tab_players (tab_id, player_id, nickname, kills, deaths, assists, score, is_winner, team_name, is_excused)
                    VALUES (@TabId, @PlayerId, @Nickname, @Kills, @Deaths, @Assists, @Score, @Winner, @Team, 0)
                    """,
                    new
                    {
                        TabId = tabId,
                        PlayerId = player.PlayerId,
                        Nickname = player.Nickname.Trim(),
                        player.Kills,
                        player.Deaths,
                        player.Assists,
                        player.Score,
                        Winner = isWinner,
                        Team = request.Team1Name
                    },
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
}
