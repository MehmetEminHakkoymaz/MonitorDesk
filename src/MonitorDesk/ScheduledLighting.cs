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
        var card = new StackPanel();
        card.Children.Add(Text(L.Get("Scheduled lighting"), 16));
        var enabled = new CheckBox { Content = L.Get("Switch Night / Normal automatically"), IsChecked = schedule.Enabled,
            IsEnabled = preferencesStore != null, Margin = new(0, 12, 0, 18) };
        enabled.SetResourceReference(StyleProperty, "SettingsCheckBox");
        enabled.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "Ink"); card.Children.Add(enabled);
        var night = new TimePicker(schedule.NightMinute);
        var day = new TimePicker(schedule.DayMinute);
        var times = new Grid(); times.ColumnDefinitions.Add(new()); times.ColumnDefinitions.Add(new());
        void AddTime(string name, string hint, TimePicker picker, int column)
        {
            var panel = new StackPanel { Margin = new(column == 0 ? 0 : 12, 0, 0, 0) };
            panel.Children.Add(Text(name, 14)); picker.Margin = new(0, 8, 0, 6); panel.Children.Add(picker);
            panel.Children.Add(Text(hint, 12, "Muted")); Grid.SetColumn(panel, column); times.Children.Add(panel);
        }
        AddTime(L.Get("Night"), L.Get("Eye comfort on"), night, 0);
        AddTime(L.Get("Normal"), L.Get("Eye comfort off"), day, 1);
        card.Children.Add(times);
        var detail = Text(L.Get("Local time · Scroll or type to choose. Monilivo must be running in the tray."), 12, "Muted");
        detail.Margin = new(0, 18, 0, 12); card.Children.Add(detail);
        var apply = new Button { Content = L.Get("Save schedule"), HorizontalAlignment = HorizontalAlignment.Left,
            IsEnabled = preferencesStore != null };
        apply.Click += async (_, _) =>
        {
            int n = night.MinuteOfDay, d = day.MinuteOfDay;
            if (n == d)
            { detail.Text = L.Get("Enter two different times in HH:mm format, for example 22:00 and 07:00."); return; }
            preferences = preferences with { Schedule = new(enabled.IsChecked == true, n, d) };
            scheduleTransitions.Reset(); SavePreferences();
            detail.Text = L.Get("Schedule saved. Enabled schedules apply the current period now and after startup or sleep.");
            await EvaluateLightingScheduleAsync();
        };
        card.Children.Add(apply); body.Children.Add(SettingsCard(card));
    }
}
