using Microsoft.AspNetCore.Mvc;
using ZoneClan.Core.Features.Attendance.ClanStats.Models;
using ZoneClan.Core.Features.Attendance.Excuses.Models;
using ZoneClan.Core.Features.Attendance.PlayerStats.Models;
using ZoneClan.Core.Features.Attendance.TabsList.Models;
using ZoneClan.Core.Features.Attendance.UploadTab.Models;
using ZoneClan.Server.Features.Attendance.ClanStats.Data;
using ZoneClan.Server.Features.Attendance.Excuses.Data;
using ZoneClan.Server.Features.Attendance.PlayerStats.Data;
using ZoneClan.Server.Features.Attendance.TabsList.Data;
using ZoneClan.Server.Features.Attendance.UploadTab.BusinessLogic;
using ZoneClan.Server.Features.Attendance.UploadTab.Data;
using ZoneClan.Server.Features.Attendance.UploadTab.Validation;

namespace ZoneClan.Server.Features.Attendance;

[ApiController]
[Route("api/attendance")]
public class AttendanceController(
    PreviewTabHandler preview,
    SaveTabHandler save,
    UpdateTabHandler update,
    WeekDeskStore weeks,
    AttendanceReadStore players,
    PlayerCardStore cards,
    AttendanceStatsStore stats,
    ExcuseStore excuses) : ControllerBase
{
    [HttpPost("preview")]
    [RequestSizeLimit(ValidateTabUpload.MaxFileSize)]
    public async Task<ActionResult<AttendancePreviewDto>> Preview(IFormFile file, [FromForm] DateOnly weekStart, [FromForm] int stage)
    {
        try
        {
            return Ok(await preview.ExecuteAsync(file, weekStart, stage));
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("tabs")]
    public async Task<IActionResult> Save([FromBody] SaveTabRequest request)
    {
        try
        {
            var tabId = await save.ExecuteAsync(request);
            return Ok(new { tabId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("tabs/{tabId:int}")]
    public async Task<IActionResult> Update(int tabId, [FromBody] SaveTabRequest request)
    {
        try
        {
            await update.ExecuteAsync(tabId, request);
            return Ok(new { tabId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("weeks/{date}")]
    public async Task<ActionResult<WeekDeskDto>> Week(DateOnly date)
    {
        var roster = await players.GetPlayersAsync();
        return Ok(await weeks.GetAsync(date, roster));
    }

    [HttpPatch("tabs/{tabId:int}/players/{rowId:int}")]
    public async Task<IActionResult> PatchRow(int tabId, int rowId, [FromBody] UpdatePlayerRowRequest request)
    {
        var updated = await weeks.SetExcuseAsync(tabId, rowId, request.IsExcused);
        return updated ? Ok() : NotFound(new { error = "Строка не найдена" });
    }

    [HttpDelete("tabs/{tabId:int}")]
    public async Task<IActionResult> Delete(int tabId)
    {
        var deleted = await weeks.DeleteTabAsync(tabId);
        return deleted ? Ok() : NotFound(new { error = "Таб не найден" });
    }

    [HttpPut("players/{playerId:long}/characteristic")]
    public async Task<IActionResult> UpdateCharacteristic(long playerId, [FromBody] int value)
    {
        await players.UpdateCharacteristicAsync((ulong)playerId, value);
        return Ok();
    }

    [HttpGet("players")]
    public async Task<ActionResult<List<PlayerListRowDto>>> Players([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (start, end) = Range(from, to);
        var roster = await players.GetPlayersAsync();
        var (list, _) = await stats.GetAsync(start, end, roster);
        return Ok(list);
    }

    [HttpGet("clan")]
    public async Task<ActionResult<ClanStatsDto>> Clan([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (start, end) = Range(from, to);
        var roster = await players.GetPlayersAsync();
        var (_, clan) = await stats.GetAsync(start, end, roster);
        return Ok(clan);
    }

    [HttpGet("excuses")]
    public async Task<ActionResult<ExcuseMatrixDto>> Excuses([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (start, end) = Range(from, to);
        var roster = await players.GetPlayersAsync();
        return Ok(await excuses.GetAsync(start, end, roster));
    }

    [HttpPut("tabs/{tabId:int}/excuse")]
    public async Task<IActionResult> SetExcuse(int tabId, [FromBody] SetExcuseRequest request)
    {
        try
        {
            await excuses.SetAsync(tabId, request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("tabs/{tabId:int}/excuse/bulk")]
    public async Task<IActionResult> SetBulkExcuse(int tabId, [FromBody] BulkExcuseRequest request)
    {
        try
        {
            request.TabId = tabId; // Принудительно ставим TabId из URL
            await excuses.SetBulkAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("players/{playerId:long}")]
    public async Task<ActionResult<PlayerCardDto>> Player(long playerId, [FromQuery] string? nickname, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.Today);
        var start = from ?? end.AddDays(-56);
        var card = await cards.GetAsync(playerId > 0 ? (ulong)playerId : null, nickname, start, end);
        return card is null ? NotFound(new { error = "Игрок не найден" }) : Ok(card);
    }

    private static (DateOnly From, DateOnly To) Range(DateOnly? from, DateOnly? to)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.Today);
        var start = from ?? end.AddDays(-56);
        return (start, end);
    }
}
