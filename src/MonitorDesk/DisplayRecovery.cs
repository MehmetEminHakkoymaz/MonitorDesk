using System.Windows.Input;
using System.Windows.Threading;
using MonitorDesk.Services;

namespace MonitorDesk;

public partial class MainWindow
{
    private readonly RecoverySchedule recovery = new();
    private readonly DispatcherTimer recoveryTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private void InitializeRecovery()
    {
        recoveryTimer.Tick += async (_, _) => await RecoverControlsAsync();
        recoveryTimer.Start();
        Closed += (_, _) => recoveryTimer.Stop();
    }
    private async Task RecoverControlsAsync()
    {
        if (closing || IsBusy || changingMode || Mouse.Captured != null) return;
        recovery.Sync(displays, DateTime.UtcNow);
        var target = recovery.Next(DateTime.UtcNow);
        if (target == null) return;
        SetBusy(true);
        try
        {
            var updated = await service.RecoverAsync(displays.ToArray(), target);
            if (closing) return;
            displays = updated;
            Render();
            if (!displays.Any(RecoverySchedule.NeedsRecovery))
                Status.Text = L.Get("Monitor connection restored. Controls are ready.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("Automatic monitor recovery failed: {0}", ex.Message);
        }
        finally
        {
            recovery.Attempted(target, DateTime.UtcNow);
            recovery.Sync(displays, DateTime.UtcNow);
            if (!closing) SetBusy(false);
        }
    }
}
