using ZoneClan.Core.Features.Attendance.TabsList.Models;
using ZoneClan.Core.Features.Attendance.PlayerStats.Models;
using ZoneClan.Core.Features.Attendance.UploadTab.Models;
using ZoneClan.Web.Features.Attendance.Data;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ZoneClan.Web.Features.Attendance.WeekDesk.BusinessLogic;

public class WeekDeskService(AttendanceApi api, ILogger<WeekDeskService> logger)
{
    public async Task<WeekDeskDto> GetWeekAsync(DateOnly date) { try { return await api.GetWeekAsync(date); } catch (Exception ex){logger.LogError(ex,"WeekDeskService.GetWeekAsync"); throw;} }
    public async Task<AttendancePreviewDto> PreviewAsync(Stream file, string fileName, DateOnly weekStart, int stage) { try { return await api.PreviewAsync(file, fileName, weekStart, stage); } catch (Exception ex){logger.LogError(ex,"WeekDeskService.PreviewAsync"); throw;} }
    public async Task SaveAsync(AttendancePreviewDto draft) { try { await api.SaveAsync(draft); } catch (Exception ex){logger.LogError(ex,"WeekDeskService.SaveAsync"); throw;} }
    public async Task UpdateTabAsync(int tabId, SaveTabRequest request) { try { await api.UpdateTabAsync(tabId, request); } catch (Exception ex){logger.LogError(ex,"WeekDeskService.UpdateTabAsync"); throw;} }
    public async Task SetExcuseAsync(int tabId, int rowId, bool isExcused) { try { await api.SetExcuseAsync(tabId, rowId, isExcused); } catch (Exception ex){logger.LogError(ex,"WeekDeskService.SetExcuseAsync"); throw;} }
    public async Task DeleteTabAsync(int tabId) { try { await api.DeleteTabAsync(tabId); } catch (Exception ex){logger.LogError(ex,"WeekDeskService.DeleteTabAsync"); throw;} }
    public async Task<PlayerCardDto> GetPlayerAsync(ulong? playerId, string? nickname, DateOnly from, DateOnly to) { try { return await api.GetPlayerAsync(playerId, nickname, from, to); } catch (Exception ex){logger.LogError(ex,"WeekDeskService.GetPlayerAsync"); throw;} }
}
