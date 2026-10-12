using MonitorDesk.Services;

internal static class ScheduleChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var schedule = new LightingSchedule(true);
        DateTime day = new(2026, 10, 12);
        check(schedule.PeriodAt(day.AddHours(22)) == new SchedulePeriod(true, day.AddHours(22)), "Night starts at the exact evening boundary");
        check(schedule.PeriodAt(day.AddHours(7)) == new SchedulePeriod(false, day.AddHours(7)), "Normal starts at the exact morning boundary");
        check(schedule.PeriodAt(day.AddHours(6)).Started == day.AddDays(-1).AddHours(22) && schedule.PeriodAt(day.AddHours(6)).Night,
            "A night period spans midnight without an extra transition");
        var reversed = new LightingSchedule(true, 8 * 60, 17 * 60);
        check(reversed.PeriodAt(day.AddHours(10)).Night && !reversed.PeriodAt(day.AddHours(20)).Night,
            "Custom same-day night periods are supported");
        check(!new LightingSchedule(true, -1, 420).Validated().Enabled && !new LightingSchedule(true, 400, 400).Validated().Enabled,
            "Invalid or equal schedule times disable the schedule safely");
        var transitions = new ScheduleTransitions();
        var night = transitions.Pending(schedule, day.AddHours(23), false)!;
        check(night.Night, "Late startup catches up to the current period");
        transitions.MarkApplied(night);
        check(transitions.Pending(schedule, day.AddDays(1).AddHours(1), false) == null,
            "Repeated ticks preserve manual changes during the same night");
        check(transitions.Pending(schedule, day.AddDays(1).AddHours(8), true) == null,
            "Busy hardware operations defer the next transition");
        var morning = transitions.Pending(schedule, day.AddDays(1).AddHours(8), false)!;
        check(!morning.Night, "A deferred transition applies once controls become idle");
        transitions.MarkApplied(morning);
        check(transitions.Pending(schedule, day.AddDays(2).AddHours(9), false)?.Started == day.AddDays(2).AddHours(7),
            "Resume after multiple missed boundaries uses the latest period");
        check(transitions.Pending(new(), day, false) == null, "Disabled schedules never request hardware changes");
        transitions.Reset();
        check(transitions.Pending(schedule, day.AddDays(1).AddHours(8), false) != null, "Changing schedule settings reevaluates the current period");
    }
}
