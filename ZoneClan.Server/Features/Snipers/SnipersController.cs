using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using ZoneClan.Core.Features.Tickets.Models;

namespace ZoneClan.Server.Features.Snipers;

[ApiController]
[Route("api/snipers")]
public class SnipersController(IConfiguration config) : ControllerBase
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    [HttpGet]
    public async Task<IActionResult> List()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<SniperInfo>(
            "SELECT id AS Id, name AS Name FROM snipers ORDER BY name");
        return Ok(rows);
    }
}
