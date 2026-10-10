using MonitorDesk.Services;

internal static class LevelChangeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var display = new Display("a", "a", "a", 1, true, 0, 0, 1920, 1080, 60, 0,
            new(0, 50, 100), new(0, 70, 100), null, "test");
        var queue = new LevelChanges();
        var now = DateTime.UtcNow;
        queue.Queue(display, false, 60, now);
        queue.Queue(display, false, 80, now.AddMilliseconds(100));
        check(queue.Count == 1 && !queue.TryTake(now.AddMilliseconds(349), out _), "Rapid slider changes coalesce and reset the quiet period");
        check(queue.TryTake(now.AddMilliseconds(350), out var change) && change!.Value == 80 && queue.Count == 0,
            "Only the newest slider value is sent after the pause");
        queue.Queue(display, false, 50, now);
        check(queue.TryTake(now.AddMilliseconds(250), out change) && change!.Value == 50,
            "Returning to the original value still queues a write after an in-flight change");
        queue.Queue(display, false, 40, now);
        queue.Queue(display, true, 60, now);
        queue.Queue(display with { Id = "b" }, false, 30, now);
        check(queue.Count == 3, "Brightness, contrast and separate monitors keep independent pending values");
        var selected = new List<LevelChange>();
        while (queue.TryTake(now.AddMilliseconds(250), out change)) selected.Add(change!);
        check(selected.Count == 3 && selected.Single(c => c.Contrast).Value == 60,
            "Ready requests drain one at a time without dropping other controls");
        var panel = display with { WmiInstance = "panel" };
        queue.Queue(panel, false, 20, now);
        queue.Queue(panel with { PhysicalIndex = 1 }, false, 30, now);
        check(queue.Count == 1, "Duplicated WMI brightness views target one hardware control");
        queue.Clear();
        check(queue.Count == 0 && !queue.TryTake(now.AddSeconds(1), out _), "Exit or failure clears unsent changes");
        foreach (var invalid in new[] { display with { Brightness = null }, display with { Brightness = new(0, 50, 100, true) } })
        {
            try { queue.Queue(invalid, false, 70, now); throw new Exception("Unavailable control queued"); }
            catch (ArgumentOutOfRangeException) { check(queue.Count == 0, "Unavailable or stale controls never queue a hardware write"); }
        }
        try { queue.Queue(display, false, 101, now); throw new Exception("Out-of-range value queued"); }
        catch (ArgumentOutOfRangeException) { check(queue.Count == 0, "Out-of-range automatic changes reject before hardware access"); }
    }
}
