namespace MonitorDesk.Services;

internal sealed class WakeRecovery
{
    private HashSet<string> expected = [];
    private DateTime due;
    private int attempts;
    internal bool Pending { get; private set; }
    internal bool Settling => Pending && attempts == 0;
    internal static string Identity(Display display) => $"{display.Id}|{display.PhysicalIndex}";
    internal void Request(IEnumerable<Display> displays, DateTime now)
    {
        if (Pending) { expected.UnionWith(displays.Select(Identity)); return; }
        expected = displays.Select(Identity).ToHashSet(); attempts = 0;
        due = now.AddSeconds(5); Pending = true;
    }
    internal void Reset() => Pending = false;
    internal bool Ready(DateTime now, bool busy) => Pending && !busy && now >= due;
    internal void Attempted(IEnumerable<Display> found, DateTime now)
    {
        var identities = found.Select(Identity).ToHashSet();
        attempts++;
        if ((identities.Count > 0 && expected.IsSubsetOf(identities)) || attempts >= 3) { Pending = false; return; }
        due = now.AddSeconds(attempts == 1 ? 15 : 30);
    }
}
