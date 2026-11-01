using Microsoft.AspNetCore.Mvc;
using ZoneClan.Core.Features.Penalties.Models;
using ZoneClan.Server.Features.Penalties.Data;

namespace ZoneClan.Server.Features.Penalties;

[ApiController]
[Route("api/penalties")]
public class PenaltiesController(PenaltiesStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PenaltyPageDto>> List([FromQuery] bool resolved = false) =>
        Ok(await store.GetAsync(resolved));

    [HttpPost]
    public async Task<IActionResult> Issue([FromBody] IssuePenaltyRequest request)
    {
        try
        {
            await store.IssueAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePenaltyRequest request)
    {
        await store.UpdateAsync(id, request);
        return Ok();
    }

    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolvePenaltyRequest request)
    {
        var updated = await store.ResolveAsync(id, request.Method);
        return updated ? Ok() : NotFound(new { error = "Штраф не найден или уже погашен" });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var updated = await store.CancelAsync(id);
        return updated ? Ok() : NotFound(new { error = "Штраф не найден" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await store.DeleteAsync(id);
        return deleted ? Ok() : BadRequest(new { error = "Можно удалять только активные штрафы" });
    }
}