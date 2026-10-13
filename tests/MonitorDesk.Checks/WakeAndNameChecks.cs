using MonitorDesk.Services;

internal static class WakeAndNameChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var display = new Display("monitor-a", "DISPLAY1", "Generic PnP Monitor", 1, true, 0, 0, 1920, 1080, 60, 0, new(0, 50, 100), new(0, 60, 100), null, "");
        DateTime now = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
        var wake = new WakeRecovery(); wake.Request([display], now);
        check(wake.Settling && !wake.Ready(now.AddSeconds(4), false) && wake.Ready(now.AddSeconds(5), false), "Resume waits five seconds before rediscovering hardware");
        wake.Request([display], now.AddSeconds(4));
        check(wake.Ready(now.AddSeconds(5), false), "Repeated display events do not postpone wake recovery indefinitely");
        check(!wake.Ready(now.AddHours(1), true), "Wake rediscovery waits for active hardware operations");
        wake.Attempted([], now.AddSeconds(5));
        check(!wake.Settling && !wake.Ready(now.AddSeconds(19), false) && wake.Ready(now.AddSeconds(20), false), "Missing monitors get a bounded 15-second rediscovery delay");
        wake.Attempted([], now.AddSeconds(20));
        check(!wake.Ready(now.AddSeconds(49), false) && wake.Ready(now.AddSeconds(50), false), "Second missing-monitor retry waits thirty seconds");
        wake.Attempted([], now.AddSeconds(50));
        check(!wake.Pending, "Rediscovery stops after three attempts even when monitors stay disconnected");
        wake.Request([display], now);
        wake.Attempted([display with { Number = 3, Device = "DISPLAY3" }], now.AddSeconds(5));
        check(!wake.Pending, "Returned monitor identity survives display renumbering");
        wake.Request([], now); wake.Attempted([display], now.AddSeconds(5));
        check(!wake.Pending, "Previously empty display discovery finishes when a monitor appears");
        var aliases = MonitorAliases.Validate([new("monitor-a", 0, " Ana ekran "), new("monitor-a", 0, "Duplicate"), new("monitor-a", 1, "OMEN"), new("other", 0, "\ninvalid"), new("", 0, "bad")]);
        check(aliases?.Count == 2 && MonitorAliases.Name(display, aliases) == "Ana ekran", "Monitor names are trimmed, invalid entries dropped and identities deduplicated");
        check(MonitorAliases.Name(display with { Number = 4, Device = "DISPLAY4" }, aliases) == "Ana ekran", "Custom names follow monitor identity rather than display number");
        check(MonitorAliases.Name(display with { PhysicalIndex = 1 }, aliases) == "OMEN", "Physical monitors sharing a logical display retain independent names");
        check(MonitorAliases.Name(display with { Id = "unknown" }, aliases) == display.Name && MonitorAliases.Name(display, []) == display.Name,
            "Unknown monitors and reset names fall back to original hardware descriptions");
    }
}
