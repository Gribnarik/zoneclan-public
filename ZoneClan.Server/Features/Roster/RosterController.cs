using Microsoft.AspNetCore.Mvc;
using ZoneClan.Core.Features.Roles.Models;
using ZoneClan.Core.Features.Roster.Models;
using ZoneClan.Server.Features.Attendance.PlayerStats.Data;
using ZoneClan.Server.Features.Roles.Data;
using ZoneClan.Server.Features.Roster.Data;

namespace ZoneClan.Server.Features.Roster;

[ApiController]
[Route("api/roster")]
public class RosterController(RosterStore store, PlayerCardStore cards, RolesStore roles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await store.ListAsync());

    [HttpGet("{discordId:long}")]
    public async Task<ActionResult<RosterCardDto>> Get(long discordId)
    {
        var card = await store.GetAsync((ulong)discordId);
        if (card is null)
            return NotFound(new { error = "Игрок не найден" });

        var end = DateOnly.FromDateTime(DateTime.Today);
        var lines = await cards.GetAsync((ulong)discordId, null, end.AddDays(-56), end);
        card.Lines = lines?.Lines ?? [];
        return Ok(card);
    }

    [HttpPatch("{discordId:long}")]
    public async Task<IActionResult> Update(long discordId, [FromBody] UpdateRosterRequest request)
    {
        try
        {
            var savedId = await store.UpdateAsync((ulong)discordId, request);
            return savedId == 0
                ? NotFound(new { error = "Игрок не найден" })
                : Ok(new RosterSaveResult { DiscordId = savedId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{discordId:long}")]
    public async Task<IActionResult> Delete(long discordId) =>
        await store.DeleteAsync((ulong)discordId)
            ? NoContent()
            : NotFound(new { error = "Игрок не найден" });

    [HttpPut("{discordId:long}/roles")]
    public async Task<IActionResult> SetRoles(long discordId, [FromBody] SetPlayerRolesRequest request)
    {
        await roles.SetPlayerRolesAsync((ulong)discordId, request.RoleIds);
        return Ok();
    }
}
