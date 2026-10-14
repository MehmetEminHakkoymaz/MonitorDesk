using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MonitorDesk.Services;

namespace MonitorDesk;

public partial class MainWindow
{
    private readonly HttpClient updateClient = new() { Timeout = TimeSpan.FromSeconds(12) };
    private readonly CancellationTokenSource updateCancellation = new();
    private readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromHours(12) };
    private static readonly Version appVersion = typeof(MainWindow).Assembly.GetName().Version!;
    private PublishedUpdate? availableUpdate;
    private string updateStatus = L.Get("Check GitHub for a newer published version.");
    private bool checkingUpdates;
    internal event EventHandler? UpdateAvailable;
    private event EventHandler? UpdateStateChanged;
    internal string AvailableUpdateLabel => availableUpdate?.Version.ToString() ?? "";
    internal void SetDiagnosticUpdate()
    {
        if (preferencesStore != null) throw new InvalidOperationException("Only available in read-only diagnostics.");
        availableUpdate = new(new Version(0, 23, 0), new Uri(UpdateChecker.ReleasesPage + "/tag/v0.23.0"));
        UpdateNotice.Visibility = Visibility.Visible; UpdateMessage.Text = L.Format("Monilivo {0} is available.", availableUpdate.Version);
    }

    private void InitializeUpdateChecks()
    {
        updateTimer.Tick += async (_, _) => { if (preferences.AutoCheckUpdates) await CheckUpdatesAsync(); };
        updateTimer.Start();
        Closed += (_, _) => { updateTimer.Stop(); updateCancellation.Cancel(); updateClient.Dispose(); };
    }
    private async Task CheckUpdatesAsync()
    {
        if (preferencesStore == null || closing || checkingUpdates || installingUpdate) return;
        checkingUpdates = true; updateStatus = L.Get("Checking for updates…"); UpdateStateChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            var published = await new UpdateChecker(updateClient).CheckAsync(updateCancellation.Token);
            if (closing) return;
            bool notify = published != null && UpdateChecker.IsNewer(published.Version, appVersion) && availableUpdate?.Version != published.Version;
            availableUpdate = published != null && UpdateChecker.IsNewer(published.Version, appVersion) ? published : null;
            updateStatus = published == null ? L.Get("No stable release has been published yet.") : availableUpdate != null
                ? L.Format("Monilivo {0} is available.", published.Version) : L.Format("No newer published version. Latest release: {0}.", published.Version);
            UpdateNotice.Visibility = availableUpdate == null ? Visibility.Collapsed : Visibility.Visible;
            UpdateMessage.Text = updateStatus;
            UpdateInstallButton.Content = L.Get(availableUpdate?.Package != null ? "Install update" : "Download page");
            if (notify) UpdateAvailable?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) { if (!closing) updateStatus = L.Get("Update check timed out. Try again later."); }
        catch (Exception) { if (!closing) updateStatus = L.Get("Could not check for updates. Check your connection or try again later."); }
        finally { checkingUpdates = false; if (!closing) UpdateStateChanged?.Invoke(this, EventArgs.Empty); }
    }
    internal void OpenReleasePage()
    {
        try { Process.Start(new ProcessStartInfo(availableUpdate?.Page.AbsoluteUri ?? UpdateChecker.ReleasesPage) { UseShellExecute = true }); }
        catch (Exception) { Status.Text = L.Get("Could not open the browser. Visit the GitHub Releases page."); }
    }
    private async void OpenRelease_Click(object sender, RoutedEventArgs e)
    { if (availableUpdate?.Package == null) OpenReleasePage(); else await InstallUpdateAsync(); }

    private void AddUpdateSettings(StackPanel body)
    {
        var card = new StackPanel(); card.Children.Add(Text(L.Get("Updates"), 16));
        var current = Text(L.Format("Installed version: {0}", new Version(appVersion.Major, appVersion.Minor, appVersion.Build)), 12, "Muted");
        current.Margin = new(0, 8, 0, 12); card.Children.Add(current);
        var automatic = new CheckBox { Content = L.Get("Check for updates automatically"), IsChecked = preferences.AutoCheckUpdates,
            IsEnabled = preferencesStore != null, Margin = new(0, 0, 0, 10) };
        automatic.SetResourceReference(StyleProperty, "SettingsCheckBox"); card.Children.Add(automatic);
        automatic.Click += async (_, _) => { preferences = preferences with { AutoCheckUpdates = automatic.IsChecked == true }; SavePreferences(); if (preferences.AutoCheckUpdates) await CheckUpdatesAsync(); };
        var detail = Text(updateStatus, 12, "Muted"); card.Children.Add(detail);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0, 12, 0, 0) };
        var check = new Button { Content = L.Get("Check now"), IsEnabled = preferencesStore != null && !checkingUpdates, Margin = new(0, 0, 8, 0) };
        var download = new Button { Content = L.Get("Download page"), IsEnabled = preferencesStore != null };
        var install = new Button { Content = L.Get("Install update"), IsEnabled = availableUpdate?.Package != null, Margin = new(8, 0, 0, 0) };
        install.Click += async (_, _) => await InstallUpdateAsync();
        check.Click += async (_, _) => await CheckUpdatesAsync(); download.Click += (_, _) => OpenReleasePage();
        actions.Children.Add(check); actions.Children.Add(download); actions.Children.Add(install); card.Children.Add(actions);
        var border = SettingsCard(card);
        EventHandler changed = (_, _) => { detail.Text = updateStatus; check.IsEnabled = preferencesStore != null && !checkingUpdates && !installingUpdate; install.IsEnabled = availableUpdate?.Package != null && !installingUpdate; };
        border.Loaded += (_, _) => UpdateStateChanged += changed;
        border.Unloaded += (_, _) => UpdateStateChanged -= changed;
        body.Children.Add(border);
    }
}
