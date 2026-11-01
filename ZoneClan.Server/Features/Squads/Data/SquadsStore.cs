using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Roles.Models;
using ZoneClan.Core.Features.Squads.Models;

namespace ZoneClan.Server.Features.Squads.Data;

public class SquadsStore(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<IReadOnlyList<SquadDto>> ListAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var squads = (await connection.QueryAsync<SquadDto>(
            """
            SELECT id AS Id, name AS Name, IFNULL(description, '') AS Description
            FROM squads
            ORDER BY name
            """)).ToList();
        var members = (await connection.QueryAsync<MemberRow>(
            """
            SELECT m.id AS Id, m.squad_id AS SquadId, m.player_id AS PlayerId,
                   p.ingame_name AS Nickname, m.squad_role AS Role, m.extra_role AS ExtraRole
            FROM squad_members m
            LEFT JOIN players p ON p.discord_id = m.player_id
            ORDER BY FIELD(m.squad_role, 'commander', 'deputy', 'fighter'), p.ingame_name
            """)).ToList();
        var roles = (await connection.QueryAsync<MemberRoleRow>(
            """
            SELECT pr.player_id AS PlayerId, r.id AS Id, r.name AS Name, r.color AS Color
            FROM player_roles pr
            JOIN roles r ON r.id = pr.role_id
            """)).ToList();

        foreach (var squad in squads)
            squad.Members = members.Where(member => member.SquadId == squad.Id).Select(member => new SquadMemberDto
            {
                Id = member.Id,
                PlayerId = member.PlayerId,
                Nickname = member.Nickname,
                Role = member.Role,
                ExtraRole = member.ExtraRole,
                Roles = roles
                    .Where(role => role.PlayerId == member.PlayerId)
                    .Select(role => new RoleDto { Id = role.Id, Name = role.Name, Color = role.Color })
                    .ToList()
            }).ToList();
        return squads;
    }

    public async Task<bool> UpdateAsync(int squadId, UpdateSquadRequest request)
    {
        var trimmed = request.Name.Trim();
        if (trimmed.Length == 0)
            throw new InvalidOperationException("Имя отряда не может быть пустым");

        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync(
            "UPDATE squads SET name = @Name, description = @Description WHERE id = @Id",
            new { Id = squadId, Name = trimmed, Description = request.Description.Trim() });
        return updated > 0;
    }

    public async Task AddMemberAsync(int squadId, AddSquadMemberRequest request)
    {
        if (request.PlayerId == 0)
            throw new InvalidOperationException("Выберите игрока");
        var role = Normalize(request.Role);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO squad_members (squad_id, player_id, squad_role)
            VALUES (@SquadId, @PlayerId, @Role)
            ON DUPLICATE KEY UPDATE squad_id = @SquadId, squad_role = @Role
            """,
            new { SquadId = squadId, request.PlayerId, Role = role });
    }

    public async Task<bool> UpdateExtraRoleAsync(int memberId, string extraRole)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync("UPDATE squad_members SET extra_role = @Extra WHERE id = @Id", new { Id = memberId, Extra = extraRole.Trim() });
        return updated > 0;
    }

    public async Task<bool> UpdateRoleAsync(int memberId, string role)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync(
            "UPDATE squad_members SET squad_role = @Role WHERE id = @Id",
            new { Id = memberId, Role = Normalize(role) });
        return updated > 0;
    }

    public async Task<bool> RemoveMemberAsync(int memberId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync("DELETE FROM squad_members WHERE id = @Id", new { Id = memberId });
        return updated > 0;
    }

    public async Task<IReadOnlyList<SquadBuildDto>> GetBuildsAsync(int squadId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        return (await connection.QueryAsync<SquadBuildDto>(
            "SELECT id AS Id, squad_id AS SquadId, IFNULL(description,'') AS Description, IFNULL(file_path,'') AS FilePath, uploaded_at AS UploadedAt FROM squad_builds WHERE squad_id = @Id ORDER BY uploaded_at DESC",
            new { Id = squadId })).ToList();
    }

    public async Task<int> AddBuildAsync(int squadId, string description, string filePath)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "INSERT INTO squad_builds (squad_id, description, file_path) VALUES (@SquadId, @Desc, @Path)",
            new { SquadId = squadId, Desc = description, Path = filePath });
        return await connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()");
    }

    public async Task<bool> DeleteBuildAsync(int buildId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var updated = await connection.ExecuteAsync("DELETE FROM squad_builds WHERE id = @Id", new { Id = buildId });
        return updated > 0;
    }

    private static string Normalize(string role) =>
        role is "commander" or "deputy" or "fighter" ? role : throw new InvalidOperationException("Роль: commander, deputy или fighter");

    private sealed class MemberRow
    {
        public int Id { get; set; }
        public int SquadId { get; set; }
        public ulong PlayerId { get; set; }
        public string Nickname { get; set; } = "";
        public string Role { get; set; } = "";
        public string ExtraRole { get; set; } = "";
    }

    private sealed class MemberRoleRow
    {
        public ulong PlayerId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "";
    }
}
