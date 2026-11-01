using Dapper;
using MySqlConnector;
using ZoneClan.Core.Features.Tickets.Models;

namespace ZoneClan.Server.Features.Armors.List.Data;

public class GetArmorsQuery(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    public async Task<IReadOnlyList<ArmorInfo>> ExecuteAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var armors = await connection.QueryAsync<ArmorInfo>(
            """
            SELECT a.id AS Id, a.name AS Name, a.category_id AS CategoryId, c.name AS CategoryName
            FROM armors a
            LEFT JOIN armor_categories c ON c.id = a.category_id
            ORDER BY a.category_id, a.name
            """);
        return armors.ToList();
    }
}
