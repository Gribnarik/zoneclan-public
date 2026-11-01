using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Roles.Models;

namespace ZoneClan.Server.Features.Roles.Data;

public class RolesStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<IReadOnlyList<RoleDto>> ListAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<RoleDto>(
            "SELECT id AS Id, name AS Name, color AS Color FROM roles ORDER BY name");
        return rows.ToList();
    }

    public async Task<RoleDto> CreateAsync(UpsertRoleRequest request)
    {
        var name = NormalizeName(request.Name);
        var color = NormalizeColor(request.Color);
        await using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "INSERT INTO roles (name, color) VALUES (@Name, @Color)",
            new { Name = name, Color = color });
        var id = await connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
        return new RoleDto { Id = id, Name = name, Color = color };
    }

    public async Task<bool> UpdateAsync(int id, UpsertRoleRequest request)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync(
            "UPDATE roles SET name = @Name, color = @Color WHERE id = @Id",
            new { Id = id, Name = NormalizeName(request.Name), Color = NormalizeColor(request.Color) });
        return updated > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync("DELETE FROM roles WHERE id = @Id", new { Id = id });
        return updated > 0;
    }

    public async Task SetPlayerRolesAsync(ulong playerId, IReadOnlyList<int> roleIds)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync(
            "DELETE FROM player_roles WHERE player_id = @Id",
            new { Id = playerId },
            transaction);
        var ids = roleIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count > 0)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO player_roles (player_id, role_id)
                SELECT @Id, id FROM roles WHERE id IN @Ids
                """,
                new { Id = playerId, Ids = ids },
                transaction);
        }

        await transaction.CommitAsync();
    }

    private static string NormalizeName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            throw new InvalidOperationException("Название роли не может быть пустым");
        return trimmed;
    }

    private static string NormalizeColor(string color)
    {
        var trimmed = color.Trim();
        if (trimmed.Length == 0)
            return "#57534e";
        if (!trimmed.StartsWith('#'))
            trimmed = "#" + trimmed;
        return trimmed;
    }
}
