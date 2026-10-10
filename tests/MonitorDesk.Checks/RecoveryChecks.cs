using MonitorDesk.Services;

internal static class RecoveryChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var now = DateTime.UtcNow;
        var display = new Display("a", "a", "a", 1, true, 0, 0, 1920, 1080, 60, 0,
            new(0, 50, 100, true), new(0, 70, 100), null, "test");
        var schedule = new RecoverySchedule();
        schedule.Sync([display], now);
        check(schedule.Next(now.AddSeconds(4)) == null && schedule.Next(now.AddSeconds(5)) == RecoverySchedule.Key(display), "Recovery first retries after five seconds");
        schedule.Sync([display], now.AddSeconds(4));
        check(schedule.Next(now.AddSeconds(5)) != null, "Repeated synchronization does not postpone a pending recovery");
        var key = RecoverySchedule.Key(display);
        foreach (int delay in new[] { 15, 30, 60, 60 })
        {
            schedule.Attempted(key, now);
            check(schedule.Next(now.AddSeconds(delay - 1)) == null && schedule.Next(now.AddSeconds(delay)) == key, $"Recovery waits {delay} seconds after completion");
            now = now.AddSeconds(delay);
        }
        var healthy = display with { Brightness = new(0, 50, 100) };
        schedule.Sync([healthy], now);
        check(schedule.Next(now.AddHours(1)) == null, "Successful recovery stops polling healthy controls");
        schedule.Sync([display], now); schedule.Sync([], now);
        check(schedule.Next(now.AddHours(1)) == null, "Disconnected displays leave the recovery schedule");
        schedule.Sync([display with { Brightness = null, Contrast = null }], now);
        check(schedule.Next(now.AddHours(1)) == null, "Unsupported controls without stale values are not polled");
        schedule.Sync([display, display with { Id = "b" }], now);
        schedule.Attempted(key, now.AddSeconds(5));
        check(schedule.Next(now.AddSeconds(5)) != key, "A failing monitor does not delay another monitor's recovery deadline");
    }
}
