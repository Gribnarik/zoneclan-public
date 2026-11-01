using Microsoft.AspNetCore.Mvc;
using ZoneClan.Core.Features.Squads.Models;
using ZoneClan.Server.Features.Squads.Data;

namespace ZoneClan.Server.Features.Squads;

[ApiController]
[Route("api/squads")]
public class SquadsController(SquadsStore store) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await store.ListAsync());

    [HttpPatch("{squadId:int}")]
    public async Task<IActionResult> Rename(int squadId, [FromBody] UpdateSquadRequest request)
    {
        try
        {
            var updated = await store.UpdateAsync(squadId, request);
            return updated ? Ok() : NotFound(new { error = "Отряд не найден" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{squadId:int}/members")]
    public async Task<IActionResult> AddMember(int squadId, [FromBody] AddSquadMemberRequest request)
    {
        try
        {
            await store.AddMemberAsync(squadId, request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("members/{memberId:int}")]
    public async Task<IActionResult> UpdateRole(int memberId, [FromBody] UpdateSquadMemberRequest request)
    {
        try
        {
            var updated = await store.UpdateRoleAsync(memberId, request.Role);
            return updated ? Ok() : NotFound(new { error = "Участник не найден" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("members/{memberId:int}/extra")]
    public async Task<IActionResult> UpdateExtra(int memberId, [FromBody] string extraRole)
    {
        var updated = await store.UpdateExtraRoleAsync(memberId, extraRole ?? "");
        return updated ? Ok() : NotFound(new { error = "Участник не найден" });
    }

    [HttpDelete("members/{memberId:int}")]
    public async Task<IActionResult> Remove(int memberId)
    {
        var deleted = await store.RemoveMemberAsync(memberId);
        return deleted ? Ok() : NotFound(new { error = "Участник не найден" });
    }

    [HttpGet("{squadId:int}/builds")]
    public async Task<IActionResult> Builds(int squadId) => Ok(await store.GetBuildsAsync(squadId));

    [HttpPost("{squadId:int}/builds")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> AddBuild(int squadId, [FromForm] string description, IFormFile? file)
    {
        string path = "";
        if (file is not null && file.Length > 0)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "squads");
            Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var full = Path.Combine(dir, name);
            await using var stream = System.IO.File.Create(full);
            await file.CopyToAsync(stream);
            path = $"/uploads/squads/{name}";
        }
        var id = await store.AddBuildAsync(squadId, description ?? "", path);
        return Ok(new { id, path });
    }

    [HttpDelete("builds/{buildId:int}")]
    public async Task<IActionResult> DeleteBuild(int buildId)
    {
        var deleted = await store.DeleteBuildAsync(buildId);
        return deleted ? Ok() : NotFound();
    }
}
