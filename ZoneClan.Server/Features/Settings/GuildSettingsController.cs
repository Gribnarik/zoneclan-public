using Microsoft.AspNetCore.Mvc;
using ZoneClan.Core.Features.Settings.Models;
using ZoneClan.Server.Features.Settings.Data;

namespace ZoneClan.Server.Features.Settings;

[ApiController]
[Route("api/guilds/{guildId}/settings")]
public class GuildSettingsController(ClanSettingsStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ClanSettingsDto>> Get(ulong guildId) =>
        Ok(await store.GetAsync(guildId));

    [HttpPut("profile")]
    public async Task<IActionResult> PutProfile(ulong guildId, [FromBody] ClanProfileUpdate request)
    {
        await store.UpdateProfileAsync(guildId, request);
        return Ok();
    }

    [HttpPut("roles/{slot}")]
    public async Task<IActionResult> PutRole(ulong guildId, string slot, [FromBody] ClanRoleSlotUpdate request)
    {
        if (!ClanRoleSlots.IsKnown(slot))
            return BadRequest(new { error = "Unknown role slot" });

        await store.UpdateRoleAsync(guildId, ClanRoleSlots.Column(slot), request.RoleId);
        return Ok();
    }

    [HttpPut("reminder-channel")]
    public async Task<IActionResult> PutReminderChannel(ulong guildId, [FromBody] ulong channelId)
    {
        await store.UpdateReminderChannelAsync(guildId, channelId);
        return Ok();
    }

    [HttpPut("gathering-channel")]
    public async Task<IActionResult> PutGatheringChannel(ulong guildId, [FromBody] ulong channelId)
    {
        await store.UpdateGatheringChannelAsync(guildId, channelId);
        return Ok();
    }

    [HttpPut("penalty-channel")]
    public async Task<IActionResult> PutPenaltyChannel(ulong guildId, [FromBody] ulong channelId)
    {
        await store.UpdatePenaltyChannelAsync(guildId, channelId);
        return Ok();
    }

    [HttpPut("absent-channel")]
    public async Task<IActionResult> PutAbsentChannel(ulong guildId, [FromBody] ulong channelId)
    {
        await store.UpdateAbsentChannelAsync(guildId, channelId);
        return Ok();
    }
}
