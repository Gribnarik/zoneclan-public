using ZoneClan.Server.Features.Tickets.Create.Models;

namespace ZoneClan.Server.Features.Tickets.Create.Validation;

public static class ValidateTicketRequest
{
    public static string? Execute(TicketRequest request) =>
        string.IsNullOrWhiteSpace(request.IngameName) ? "IngameName is required" : null;
}
