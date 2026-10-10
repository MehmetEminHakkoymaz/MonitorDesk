namespace MonitorDesk.Services;

internal sealed class RecoverySchedule
{
    private readonly Dictionary<string, (int Attempt, DateTime Due)> entries = [];
    internal static string Key(Display d) => $"{d.Id}|{d.Device}|{d.PhysicalIndex}";
    internal static bool NeedsRecovery(Display d) => d.Brightness?.IsStale == true || d.Contrast?.IsStale == true;
    internal void Sync(IEnumerable<Display> displays, DateTime now)
    {
        var stale = displays.Where(NeedsRecovery).Select(Key).ToHashSet();
        foreach (var key in entries.Keys.Where(k => !stale.Contains(k)).ToArray()) entries.Remove(key);
        foreach (var key in stale) entries.TryAdd(key, (0, now.AddSeconds(5)));
    }
    internal string? Next(DateTime now) => entries.Where(e => e.Value.Due <= now)
        .OrderBy(e => e.Value.Due).Select(e => e.Key).FirstOrDefault();
    internal void Attempted(string key, DateTime now)
    {
        if (!entries.TryGetValue(key, out var old)) return;
        int attempt = Math.Min(old.Attempt + 1, 3);
        entries[key] = (attempt, now.AddSeconds(attempt switch { 1 => 15, 2 => 30, _ => 60 }));
    }
}
