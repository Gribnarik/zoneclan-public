using Microsoft.AspNetCore.Mvc;
using ZoneClan.Server.Features.Tickets.Create.BusinessLogic;
using ZoneClan.Server.Features.Tickets.Create.Models;
using ZoneClan.Server.Features.Tickets.Create.Validation;

namespace ZoneClan.Server.Features.Tickets;

[ApiController]
[Route("api/[controller]")]
public class TicketsController(CreateTicketHandler handler) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateTicket([FromBody] TicketRequest request)
    {
        var error = ValidateTicketRequest.Execute(request);
        if (error is not null)
            return BadRequest(new { error });

        try
        {
            await handler.ExecuteAsync(request);
            return Ok(new { message = "Ticket created successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
