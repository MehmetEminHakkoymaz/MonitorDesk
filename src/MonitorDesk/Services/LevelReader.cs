namespace MonitorDesk.Services;
internal record LevelReply(Level? Value, int Error);
internal sealed class LevelReader
{
    private readonly Dictionary<string, Level> lastKnown = new(StringComparer.Ordinal);
    private readonly Action<int> wait;
    internal LevelReader(Action<int>? wait = null) => this.wait = wait ?? Thread.Sleep;
    internal Level? Read(string key, Func<LevelReply> query)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            // Pace DDC requests on the worker thread; never block the UI thread.
            wait(attempt switch { 0 => 150, 1 => 350, _ => 700 });
            var reply = query();
            if (reply.Value is { } value)
            {
                lastKnown[key] = value with { IsStale = false };
                return lastKnown[key];
            }
            System.Diagnostics.Trace.TraceWarning("Monitor read failed: error=0x{0:X8}, attempt={1}", reply.Error, attempt + 1);
            if (!IsTransient(reply.Error)) break;
        }
        return Unavailable(key);
    }
    internal Level? Unavailable(string key) => lastKnown.TryGetValue(key, out var old) ? old with { IsStale = true } : null;
    internal void Retain(HashSet<string> connected)
    {
        foreach (string key in lastKnown.Keys.Where(key => !connected.Contains(key)).ToArray()) lastKnown.Remove(key);
    }
    internal static bool IsTransient(int error) => unchecked((uint)error) is
        0xC0262582 or 0xC0262583 or 0xC0262585 or 0xC026258B || error is 31 or 121 or 1460;
}
