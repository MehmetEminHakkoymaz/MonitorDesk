namespace MonitorDesk.Services;

internal record LightingSchedule(bool Enabled = false, int NightMinute = 22 * 60, int DayMinute = 7 * 60)
{
    internal LightingSchedule Validated() => NightMinute is >= 0 and < 1440 && DayMinute is >= 0 and < 1440 && NightMinute != DayMinute
        ? this : new();

    // Local wall-clock boundaries also catch up after sleep or a delayed startup.
    internal SchedulePeriod PeriodAt(DateTime localNow)
    {
        DateTime night = localNow.Date.AddMinutes(NightMinute);
        DateTime day = localNow.Date.AddMinutes(DayMinute);
        if (night > localNow) night = night.AddDays(-1);
        if (day > localNow) day = day.AddDays(-1);
        return night > day ? new(true, night) : new(false, day);
    }
}

internal record SchedulePeriod(bool Night, DateTime Started);

// Tracks periods, rather than current control values, to preserve manual changes
// until the next scheduled boundary. Busy operations leave the period pending.
internal sealed class ScheduleTransitions
{
    private SchedulePeriod? applied;
    internal SchedulePeriod? Pending(LightingSchedule schedule, DateTime now, bool busy)
    {
        if (!schedule.Enabled || busy) return null;
        var period = schedule.PeriodAt(now);
        return period == applied ? null : period;
    }
    internal void MarkApplied(SchedulePeriod period) => applied = period;
    internal void Reset() => applied = null;
}
