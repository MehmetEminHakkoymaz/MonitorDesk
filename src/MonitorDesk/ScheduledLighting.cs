using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MonitorDesk.Services;

namespace MonitorDesk;

public partial class MainWindow
{
    private readonly DispatcherTimer lightingScheduleTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private readonly ScheduleTransitions scheduleTransitions = new();
    private bool scheduleRunning;

    private void InitializeLightingSchedule()
    {
        lightingScheduleTimer.Tick += async (_, _) => await EvaluateLightingScheduleAsync();
        lightingScheduleTimer.Start();
        Closed += (_, _) => lightingScheduleTimer.Stop();
    }

    private async Task EvaluateLightingScheduleAsync()
    {
        if (preferencesStore == null || closing || !InitialRead.Task.IsCompleted || scheduleRunning) return;
        var schedule = preferences.Schedule ?? new LightingSchedule();
        var period = scheduleTransitions.Pending(schedule, DateTime.Now, IsBusy || changingMode);
        if (period == null) return;
        scheduleRunning = true;
        // One batch per boundary, including a partial hardware failure. Existing
        // recovery handles stale controls without repeatedly overwriting sliders.
        scheduleTransitions.MarkApplied(period);
        try
        {
            warmEnabled = period.Night;
            UpdateWarmFilter(); SavePreferences();
            await ApplyProfileAsync(LightingProfile.Presets[period.Night ? 0 : 1]);
        }
        finally { scheduleRunning = false; }
    }

    private void AddLightingScheduleSettings(StackPanel body)
    {
        var schedule = preferences.Schedule ?? new LightingSchedule();
        body.Children.Add(Text(L.Get("Scheduled lighting"), 16));
        var enabled = new CheckBox { Content = L.Get("Switch Night / Normal automatically"), IsChecked = schedule.Enabled,
            IsEnabled = preferencesStore != null, Margin = new(0, 12, 0, 10) };
        enabled.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "Ink"); body.Children.Add(enabled);
        var night = new TextBox { Text = FormatMinute(schedule.NightMinute), Width = 70, Margin = new(8, 0, 12, 0) };
        var day = new TextBox { Text = FormatMinute(schedule.DayMinute), Width = 70, Margin = new(8, 0, 0, 0) };
        var times = new StackPanel { Orientation = Orientation.Horizontal };
        times.Children.Add(Text(L.Get("Night"), 13)); times.Children.Add(night);
        times.Children.Add(Text(L.Get("Normal"), 13)); times.Children.Add(day); body.Children.Add(times);
        var detail = Text(L.Get("Local time (HH:mm). Night enables Eye comfort at its selected strength; Normal disables it. Monilivo must be running, including in the tray. Manual changes last until the next switch."), 12, "Muted");
        detail.Margin = new(0, 10, 0, 10); body.Children.Add(detail);
        var apply = new Button { Content = L.Get("Save schedule"), HorizontalAlignment = HorizontalAlignment.Left,
            IsEnabled = preferencesStore != null, Margin = new(0, 0, 0, 16) };
        apply.Click += async (_, _) =>
        {
            if (!TimeOnly.TryParseExact(night.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var n) ||
                !TimeOnly.TryParseExact(day.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) || n == d)
            { detail.Text = L.Get("Enter two different times in HH:mm format, for example 22:00 and 07:00."); return; }
            preferences = preferences with { Schedule = new(enabled.IsChecked == true, n.Hour * 60 + n.Minute, d.Hour * 60 + d.Minute) };
            scheduleTransitions.Reset(); SavePreferences();
            detail.Text = L.Get("Schedule saved. Enabled schedules apply the current period now and after startup or sleep.");
            await EvaluateLightingScheduleAsync();
        };
        body.Children.Add(apply);
    }
    private static string FormatMinute(int value) => $"{value / 60:00}:{value % 60:00}";
}
