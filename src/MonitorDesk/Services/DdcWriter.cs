using System.ComponentModel;

namespace MonitorDesk.Services;

// Retrying the same absolute value is idempotent, even if a transport reply was lost.
internal sealed class DdcWriter(Action<int>? pause = null)
{
    private readonly Action<int> wait = pause ?? Thread.Sleep;

    internal void Write(Func<int> command)
    {
        int error = 0;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            wait(attempt switch { 0 => 200, 1 => 500, _ => 1000 });
            error = command();
            if (error == 0)
            {
                // Allow firmware to settle before another write or read-back.
                wait(350);
                return;
            }
            System.Diagnostics.Trace.TraceWarning("Monitor write failed: error=0x{0:X8}, attempt={1}", error, attempt + 1);
            if (!LevelReader.IsTransient(error)) break;
        }
        throw new Win32Exception(error, $"Monitor did not accept the setting (Windows error 0x{unchecked((uint)error):X8}). Check DDC/CI, the monitor's picture mode and other monitor-control apps, then refresh and retry.");
    }
}
