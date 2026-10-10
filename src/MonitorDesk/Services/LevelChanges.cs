namespace MonitorDesk.Services;

internal record LevelChange(Display Display, bool Contrast, uint Value, DateTime Due);

// UI-thread queue: each hardware control keeps only its latest requested value.
internal sealed class LevelChanges
{
    private readonly Dictionary<(string, int, string?, bool), LevelChange> pending = new();
    internal int Count => pending.Count;
    internal void Queue(Display display, bool contrast, uint value, DateTime now)
    {
        var level = contrast ? display.Contrast : display.Brightness;
        if (level == null || level.IsStale || value < level.Min || value > level.Max)
            throw new ArgumentOutOfRangeException(nameof(value));
        var key = (display.Id, !contrast && display.WmiInstance != null ? -1 : display.PhysicalIndex,
            !contrast ? display.WmiInstance : null, contrast);
        pending[key] = new(display, contrast, value, now.AddMilliseconds(250));
    }
    internal bool TryTake(DateTime now, out LevelChange? change)
    {
        var next = pending.FirstOrDefault(p => p.Value.Due <= now);
        change = next.Value;
        return change != null && pending.Remove(next.Key);
    }
    internal void Clear() => pending.Clear();
}
