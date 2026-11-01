using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZoneClan.Server.Features.Attendance.UploadTab.BusinessLogic;

public class AttendanceDraft
{
    public string Id { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public DateOnly WeekStart { get; init; }
    public int Stage { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public class AttendanceDraftStore
{
    private readonly ConcurrentDictionary<string, AttendanceDraft> _drafts = new();

    public void Put(AttendanceDraft draft)
    {
        var cutoff = DateTime.UtcNow.AddHours(-6);
        // Collect stale keys first to avoid collection modification during enumeration
        var staleKeys = _drafts.Where(pair => pair.Value.CreatedAt < cutoff).Select(pair => pair.Key).ToList();
        foreach (var key in staleKeys)
            _drafts.TryRemove(key, out _);
        _drafts[draft.Id] = draft;
    }

    public AttendanceDraft? Take(string id) =>
        _drafts.TryRemove(id, out var draft) ? draft : null;
}
