using System.Collections.Concurrent;

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
        foreach (var stale in _drafts.Where(pair => pair.Value.CreatedAt < cutoff).Select(pair => pair.Key))
            _drafts.TryRemove(stale, out _);
        _drafts[draft.Id] = draft;
    }

    public AttendanceDraft? Take(string id) =>
        _drafts.TryRemove(id, out var draft) ? draft : null;
}
