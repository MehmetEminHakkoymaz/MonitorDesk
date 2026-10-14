using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using MonitorDesk.Services;

namespace MonitorDesk;

public partial class MainWindow
{
    private bool installingUpdate;
    private CancellationTokenSource? downloadCancellation;
    internal async Task InstallUpdateAsync()
    {
        if (preferencesStore == null || closing || installingUpdate) return;
        var target = availableUpdate;
        if (target?.Package is not { } package)
        { MessageBox.Show(L.Get("No installer is available for this release. Use the download page."), L.Get("Updates")); return; }
        if (IsBusy || changingMode)
        { MessageBox.Show(L.Get("Wait for the current monitor operation to finish before updating."), L.Get("Updates")); return; }
        Show(); if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal; Activate();
        if (MessageBox.Show(this, L.Format("Download and install Monilivo {0}? Monilivo will close for installation and reopen afterward. Your settings and profiles will be kept.", target.Version),
            L.Get("Install update"), MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        installingUpdate = true; SetBusy(true);
        UpdateInstallButton.IsEnabled = false; UpdateCancel.Visibility = UpdateProgress.Visibility = Visibility.Visible; UpdateProgress.Value = 0;
        UpdateStateChanged?.Invoke(this, EventArgs.Empty);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(updateCancellation.Token);
        downloadCancellation = cancellation; cancellation.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Monilivo-UpdateDownload/1.0");
            var progress = new Progress<int>(percent =>
            {
                if (closing || !installingUpdate) return;
                UpdateProgress.Value = percent;
                UpdateMessage.Text = updateStatus = L.Format("Downloading update… {0}%", percent);
                UpdateStateChanged?.Invoke(this, EventArgs.Empty);
            });
            UpdateMessage.Text = updateStatus = L.Get("Downloading and verifying the installer…");
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonitorDesk", "updates", Guid.NewGuid().ToString("N"));
            string installer = await new InstallerDownload(client).DownloadAsync(package, directory, progress, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (closing) return;
            SavePreferences();
            var start = new ProcessStartInfo(installer) { UseShellExecute = true };
            start.ArgumentList.Add("/SP-"); start.ArgumentList.Add("/NORESTART"); start.ArgumentList.Add("/MonilivoUpdate=1");
            // Only upgrade this directory silently when it belongs to an installed copy.
            // Development/portable copies use the normal installer to choose a destination.
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe")))
            { start.ArgumentList.Add("/SILENT"); start.ArgumentList.Add("/DIR=" + AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)); }
            start.ArgumentList.Add("/LOG=" + Path.Combine(directory, "install.log"));
            if (Process.Start(start) == null) throw new InvalidOperationException("Setup did not start.");
            ExitApplication();
        }
        catch (OperationCanceledException) { if (!closing) UpdateMessage.Text = updateStatus = L.Get("Update download cancelled or timed out. Monilivo is still running."); }
        catch (Exception) { if (!closing) UpdateMessage.Text = updateStatus = L.Get("The update could not be downloaded, verified or started. Monilivo is still running; try again or use the download page."); }
        finally
        {
            downloadCancellation = null; installingUpdate = false;
            if (!closing)
            {
                UpdateCancel.Visibility = UpdateProgress.Visibility = Visibility.Collapsed;
                UpdateInstallButton.IsEnabled = true; SetBusy(false); UpdateStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    private void CancelUpdate_Click(object sender, RoutedEventArgs e) => downloadCancellation?.Cancel();
}
