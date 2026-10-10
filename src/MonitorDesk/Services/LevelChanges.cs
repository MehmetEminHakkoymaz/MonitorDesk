namespace MonitorDesk.Services;

internal record LevelChange(Display Display, bool Contrast, uint Value, DateTime Due);

// UI-thread queue: each hardware control keeps only its latest requested value.
internal sealed class LevelChanges
{
    private readonly Dictionary<(string, int, string?, bool), LevelChange> pending = new();
    private readonly Dictionary<(string, int, string?, bool), DateTime> nextAllowed = new();
    internal int Count => pending.Count;
    internal void Queue(Display display, bool contrast, uint value, DateTime now)
    {
        var level = contrast ? display.Contrast : display.Brightness;
        if (level == null || level.IsStale || value < level.Min || value > level.Max)
            throw new ArgumentOutOfRangeException(nameof(value));
        var key = (display.Id, !contrast && display.WmiInstance != null ? -1 : display.PhysicalIndex,
            !contrast ? display.WmiInstance : null, contrast);
        // Preserve the dispatch deadline while dragging; new input only replaces the value.
        // The first value is eligible immediately, subsequent values are sampled at 250 ms.
        DateTime due = pending.TryGetValue(key, out var queued) ? queued.Due
            : nextAllowed.TryGetValue(key, out var next) && next > now ? next : now;
        pending[key] = new(display, contrast, value, due);
    }
    internal bool TryTake(DateTime now, out LevelChange? change)
    {
        var next = pending.FirstOrDefault(p => p.Value.Due <= now);
        change = next.Value;
        if (change == null) return false;
        pending.Remove(next.Key);
        nextAllowed[next.Key] = now.AddMilliseconds(250);
        return true;
    }
    internal void Clear() { pending.Clear(); nextAllowed.Clear(); }
}
