using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using MonitorDesk.Services;

namespace MonitorDesk;

public partial class MainWindow
{
    private Preferences preferences = new();
    private PreferencesStore? preferencesStore;
    private StartupRegistration? startupRegistration;
    private bool preferencesReady, restoringPlacement;
    private string? preferenceLoadError;
    private readonly DispatcherTimer savePreferencesTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    private void LoadPreferences(bool remember)
    {
        if (!remember) return;
        preferencesStore = new PreferencesStore();
        try { preferences = preferencesStore.Load(); }
        catch (Exception ex) { preferenceLoadError = L.Get("Could not load preferences; defaults are in use: ") + ex.Message; }
        startupRegistration = new StartupRegistration(Environment.ProcessPath!);
    }
    private void InitializePreferences()
    {
        light = preferences.LightTheme; ApplyTheme();
        WarmStrength.Value = preferences.EyeStrength;
        WarmValue.Text = L.Format("Strength: {0} / 90", preferences.EyeStrength);
        // Restore the state now; Render creates overlays after display discovery,
        // including when startup keeps the main window hidden in the tray.
        warmEnabled = preferences.EyeEnabled;
        WarmButton.Content = warmEnabled ? L.Get("Eye comfort · On") : L.Get("Eye comfort · Off");
        if (preferences.Window is { } place) { Width = place.Width; Height = place.Height; WindowStartupLocation = WindowStartupLocation.Manual; }
        SourceInitialized += (_, _) =>
        {
            restoringPlacement = true;
            try
            {
                if (preferences.Window is { } saved) RestorePlacement(saved);
                if (preferences.Maximized) WindowState = WindowState.Maximized;
            }
            finally { restoringPlacement = false; preferencesReady = true; }
        };
        LocationChanged += (_, _) => RememberWindow();
        SizeChanged += (_, _) => RememberWindow();
        StateChanged += (_, _) => RememberWindow();
        savePreferencesTimer.Tick += (_, _) => SavePreferences();
    }
    private void RememberWindow()
    {
        if (!preferencesReady || restoringPlacement || preferencesStore == null || !IsVisible) return;
        var handle = new WindowInteropHelper(this).Handle;
        if (WindowState == WindowState.Normal && GetWindowRect(handle, out var rect))
            preferences = preferences with { Window = new(rect.Left, rect.Top, Width, Height), Maximized = false };
        else if (WindowState == WindowState.Maximized) preferences = preferences with { Maximized = true };
        SchedulePreferencesSave();
    }
    private void RestorePlacement(WindowPlacement saved)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var workAreas = System.Windows.Forms.Screen.AllScreens.Select(s => s.WorkingArea)
            .Select(r => new Rect(r.X, r.Y, r.Width, r.Height)).ToArray();
        double scale = GetDpiForWindow(handle) / 96.0;
        if (scale <= 0) scale = 1;
        var fitted = PlacementGeometry.Fit(new(saved.Left, saved.Top, saved.Width * scale, saved.Height * scale), workAreas);
        Native.SetWindowPos(handle, 0, (int)fitted.Left, (int)fitted.Top, 0, 0, 0x0015);
        // Moving to another monitor can change the window DPI before its size is restored.
        scale = GetDpiForWindow(handle) / 96.0;
        if (scale <= 0) scale = 1;
        fitted = PlacementGeometry.Fit(new(saved.Left, saved.Top, saved.Width * scale, saved.Height * scale), workAreas);
        Native.SetWindowPos(handle, 0, (int)fitted.Left, (int)fitted.Top, (int)fitted.Width, (int)fitted.Height, 0x0014);
    }
    private void SchedulePreferencesSave()
    {
        if (preferencesStore == null || !preferencesReady || restoringPlacement) return;
        savePreferencesTimer.Stop(); savePreferencesTimer.Start();
    }
    private void SavePreferences()
    {
        savePreferencesTimer.Stop();
        if (preferencesStore == null) return;
        preferences = preferences with { LightTheme = light, EyeStrength = (int)WarmStrength.Value, EyeEnabled = warmEnabled };
        try { preferencesStore.Save(preferences); }
        catch (Exception ex) { Status.Text = L.Get("Could not save preferences: ") + ex.Message; }
    }
    internal async Task StartInTrayAsync()
    {
        new WindowInteropHelper(this).EnsureHandle();
        await RefreshAsync(); InitialRead.TrySetResult();
        if (preferenceLoadError != null) Status.Text = preferenceLoadError;
    }
    private void Settings_Click(object sender, RoutedEventArgs e)
        => CreateSettingsDialog().ShowDialog();
    internal Window CreateSettingsDialog()
    {
        var body = new StackPanel { Margin = new(24) };
        body.Children.Add(Text(L.Get("Preferences"), 22));
        var explanation = Text(L.Get("Theme, Eye comfort state and strength, and window position are saved automatically."), 13, "Muted");
        explanation.Margin = new(0, 12, 0, 20); body.Children.Add(explanation);
        var start = new CheckBox { Content = L.Get("Start with Windows in the system tray"), IsEnabled = startupRegistration != null };
        start.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "Ink");
        var detail = Text(L.Get("Starts when you sign in. Open the window from the tray icon."), 12, "Muted");
        detail.Margin = new(0, 8, 0, 16);
        try { start.IsChecked = startupRegistration?.Enabled == true; }
        catch (Exception ex) { start.IsEnabled = false; detail.Text = L.Get("Could not read Windows startup preference: ") + ex.Message; }
        start.Click += (_, _) =>
        {
            try { startupRegistration!.SetEnabled(start.IsChecked == true); }
            catch (Exception ex)
            {
                detail.Text = L.Get("Could not change Windows startup preference: ") + ex.Message;
                start.IsChecked = !(start.IsChecked == true);
            }
        };
        body.Children.Add(start); body.Children.Add(detail);
        var close = new Button { Content = L.Get("Close"), HorizontalAlignment = HorizontalAlignment.Right, IsCancel = true };
        body.Children.Add(close);
        var dialog = new Window { Title = L.Get("Preferences"), Owner = this, Content = body, Width = 440,
            SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        dialog.SetResourceReference(BackgroundProperty, "Page");
        close.Click += (_, _) => dialog.Close(); return dialog;
    }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out Native.Rect rect);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint window);
}
