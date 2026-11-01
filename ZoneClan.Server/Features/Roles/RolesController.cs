using Microsoft.AspNetCore.Mvc;
using ZoneClan.Core.Features.Roles.Models;
using ZoneClan.Server.Features.Roles.Data;

namespace ZoneClan.Server.Features.Roles;

[ApiController]
[Route("api/roles")]
public class RolesController(RolesStore store) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await store.ListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertRoleRequest request)
    {
        try
        {
            return Ok(await store.CreateAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertRoleRequest request)
    {
        try
        {
            var updated = await store.UpdateAsync(id, request);
            return updated ? Ok() : NotFound(new { error = "Роль не найдена" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await store.DeleteAsync(id);
        return deleted ? Ok() : NotFound(new { error = "Роль не найдена" });
    }
}
