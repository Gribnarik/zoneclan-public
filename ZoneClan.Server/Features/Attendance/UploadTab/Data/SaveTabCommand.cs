using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Attendance.UploadTab.Models;

namespace ZoneClan.Server.Features.Attendance.UploadTab.Data;

public class SaveTabCommand(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<int> ExecuteAsync(SaveTabRequest request, string imagePath, bool isValid, bool isWinner)
    {
        request.DayOfWeek = (int)request.WeekStart.DayOfWeek;
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            // Сохраняем результат (win/loss) явно по ТЗ
            string? result = isValid ? (isWinner ? "win" : "loss") : null;

            await connection.ExecuteAsync(
                                """
                INSERT INTO match_tabs (week_start, day_of_week, stage, image_path, team1_name, team1_score, team2_name, team2_score, guilds, is_valid, result)
                VALUES (@Week, @DayOfWeek, @Stage, @Image, @Team1, @Score1, @Team2, @Score2, @Guilds, @Valid, @Result)
                """,
                new
                {
                    Week = request.WeekStart.ToDateTime(TimeOnly.MinValue),
                    DayOfWeek = request.DayOfWeek, // <-- ДОБАВИТЬ
                    request.Stage,
                    Image = imagePath,
                    Team1 = request.Team1Name,
                    Score1 = request.Team1Score,
                    Team2 = request.Team2Name,
                    Score2 = request.Team2Score,
                    request.Guilds,
                    Valid = isValid,
                    Result = result
                },
                transaction);

            var tabId = await connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()", transaction: transaction);
            
            // Сортируем игроков по очкам перед сохранением
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

                if (player.PlayerId is > 0)
                {
                    await connection.ExecuteAsync(
                        """
                        INSERT INTO player_stage_status (week_start, stage, player_id, status)
                        VALUES (@Week, @Stage, @PlayerId, 'attended')
                        ON DUPLICATE KEY UPDATE status = IF(status = 'excused', status, 'attended')
                        """,
                        new
                        {
                            Week = request.WeekStart.ToDateTime(TimeOnly.MinValue),
                            request.Stage,
                            PlayerId = player.PlayerId
                        },
                        transaction);
                }
            }

            await transaction.CommitAsync();
            return tabId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}