using System.Windows.Input;
using Microsoft.Win32;
using MonitorDesk.Services;

namespace MonitorDesk;

public partial class MainWindow
{
    private readonly WakeRecovery wakeRecovery = new();
    private bool suspended;
    private void InitializeWakeHandling()
    {
        SystemEvents.PowerModeChanged += PowerChanged;
        Closed += (_, _) => SystemEvents.PowerModeChanged -= PowerChanged;
    }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (closing) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (closing) return;
            if (e.Mode == PowerModes.Suspend)
            {
                wakeRecovery.Reset();
                suspended = true; levelTimer.Stop(); levelChanges.Clear(); debounce.Stop();
                if (!levelTickRunning) autoLevelsActive = false;
                warmFilter.Dispose(); SetBusy(busy);
            }
            else if (e.Mode == PowerModes.Resume)
            {
                wakeRecovery.Reset();
                suspended = false; RequestWakeRecovery();
            }
        });
    }
    private void RequestWakeRecovery()
    {
        levelTimer.Stop(); levelChanges.Clear();
        if (!levelTickRunning) autoLevelsActive = false;
        debounce.Stop(); wakeRecovery.Request(displays, DateTime.UtcNow);
        Status.Text = L.Get("Waiting for monitors to reconnect…"); SetBusy(busy);
    }
    private async Task RecoverAfterWakeAsync()
    {
        if (closing || suspended || !InitialRead.Task.IsCompleted || !wakeRecovery.Ready(DateTime.UtcNow,
                busy || autoLevelsActive || levelTickRunning || changingMode || Mouse.Captured != null)) return;
        SetBusy(true);
        List<Display> found = [];
        try
        {
            found = await service.ReadAsync();
            if (closing) return;
            displays = found; await RefreshLayoutAsync();
            if (closing) return;
            // Removing old overlays also removes windows on disconnected screens.
            warmFilter.Dispose(); Render();
            Status.Text = L.Get("Displays synchronized after reconnecting.");
        }
        catch (Exception ex) { if (!closing) Status.Text = L.Get("Could not refresh displays: ") + L.Message(ex.Message); }
        finally
        {
            wakeRecovery.Attempted(found, DateTime.UtcNow);
            if (!closing && warmEnabled && !wakeRecovery.Settling && !suspended) UpdateWarmFilter();
            if (!closing) SetBusy(false);
        }
    }
}
