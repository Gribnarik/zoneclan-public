using ZoneClan.Core.Features.Roster.Models;
using ZoneClan.Web.Features.Roster.Data;
using Microsoft.Extensions.Logging;

namespace ZoneClan.Web.Features.Roster.BusinessLogic;

public class RosterService(RosterApi api, ILogger<RosterService> logger)
{
    public async Task<List<RosterPlayerDto>> GetListAsync()
    {
        try { return await api.GetListAsync(); }
        catch (Exception ex) { logger.LogError(ex, "RosterService.GetListAsync"); throw; }
    }
    public async Task<RosterCardDto> GetAsync(ulong discordId)
    {
        try { return await api.GetAsync(discordId); }
        catch (Exception ex) { logger.LogError(ex, "RosterService.GetAsync"); throw; }
    }
    public async Task<ulong> UpdateAsync(ulong discordId, UpdateRosterRequest request)
    {
        try { return await api.UpdateAsync(discordId, request); }
        catch (Exception ex) { logger.LogError(ex, "RosterService.UpdateAsync"); throw; }
    }
    public async Task DeleteAsync(ulong discordId)
    {
        try { await api.DeleteAsync(discordId); }
        catch (Exception ex) { logger.LogError(ex, "RosterService.DeleteAsync"); throw; }
    }
}
