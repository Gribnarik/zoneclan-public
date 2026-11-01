using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Penalties.Models;

namespace ZoneClan.Server.Features.Penalties.Data;

public class PenaltiesStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<PenaltyPageDto> GetAsync(bool includeResolved)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var types = (await connection.QueryAsync<PenaltyTypeDto>(
            """
            SELECT id AS Id, name AS Name, default_points AS DefaultPoints
            FROM penalty_types
            ORDER BY id
            """)).ToList();
            
        var sql = includeResolved
            ? """
            SELECT n.id AS Id, n.player_id AS PlayerId, IFNULL(p.ingame_name, '') AS Nickname,
                   t.name AS TypeName, IFNULL(n.reason, '') AS Reason, n.points AS Points,
                   n.status AS Status, n.issued_at AS IssuedAt, n.worked_off_method AS ResolutionMethod
            FROM player_penalties n
            JOIN penalty_types t ON t.id = n.penalty_type_id
            LEFT JOIN players p ON p.discord_id = n.player_id
            ORDER BY n.issued_at DESC
            """
            : """
            SELECT n.id AS Id, n.player_id AS PlayerId, IFNULL(p.ingame_name, '') AS Nickname,
                   t.name AS TypeName, IFNULL(n.reason, '') AS Reason, n.points AS Points,
                   n.status AS Status, n.issued_at AS IssuedAt, n.worked_off_method AS ResolutionMethod
            FROM player_penalties n
            JOIN penalty_types t ON t.id = n.penalty_type_id
            LEFT JOIN players p ON p.discord_id = n.player_id
            WHERE n.status = 'active'
            ORDER BY n.issued_at DESC
            """;
        var items = (await connection.QueryAsync<PenaltyDto>(sql)).ToList();

        var summaries = items
            .Where(item => item.Status == "active")
            .GroupBy(item => item.PlayerId)
            .Select(group => new PenaltySummaryDto
            {
                PlayerId = group.Key,
                Nickname = group.First().Nickname,
                TotalPoints = group.Sum(item => item.Points),
                ActiveCount = group.Count()
            })
            .OrderByDescending(item => item.TotalPoints)
            .ToList();

        return new PenaltyPageDto { Types = types, Items = items, Summaries = summaries };
    }

    public async Task IssueAsync(IssuePenaltyRequest request)
    {
        if (request.PlayerId == 0)
            throw new InvalidOperationException("Выберите игрока");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Укажите причину");

        await using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO player_penalties (player_id, penalty_type_id, points, reason, issued_by, status)
            VALUES (@PlayerId, @TypeId, @Points, @Reason, 0, 'active')
            """,
            new
            {
                request.PlayerId,
                request.TypeId,
                request.Points,
                Reason = request.Reason.Trim()
            });
    }

    public async Task UpdateAsync(int id, UpdatePenaltyRequest request)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(
            """
            UPDATE player_penalties 
            SET penalty_type_id = @TypeId, points = @Points, reason = @Reason 
            WHERE id = @Id AND status = 'active'
            """,
            new { Id = id, TypeId = request.TypeId, Points = request.Points, Reason = request.Reason });
    }

    public async Task<bool> ResolveAsync(int id, string method)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync(
            """
            UPDATE player_penalties
            SET status = 'worked_off', worked_off_method = @Method, worked_off_at = NOW()
            WHERE id = @Id AND status = 'active'
            """,
            new { Id = id, Method = method });
        return updated > 0;
    }

    public async Task<bool> CancelAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync(
            "UPDATE player_penalties SET status = 'cancelled' WHERE id = @Id AND status = 'active'",
            new { Id = id });
        return updated > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        // Физическое удаление разрешено только для активных (не погашенных) штрафов
        var updated = await connection.ExecuteAsync(
            "DELETE FROM player_penalties WHERE id = @Id AND status = 'active'", 
            new { Id = id });
        return updated > 0;
    }
}