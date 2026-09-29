using System.Runtime.InteropServices;

namespace MonitorDesk.Services;

internal record DisplayMode(uint Width, uint Height, uint Hertz)
{
    public override string ToString() => $"{Width} × {Height} · {Hertz} Hz";
}

internal sealed class DisplayModes
{
    internal static Native.DevMode Current(string device)
    {
        var mode = new Native.DevMode { Size = (ushort)Marshal.SizeOf<Native.DevMode>() };
        if (!Native.EnumDisplaySettings(device, -1, ref mode))
            throw new InvalidOperationException("Display is unavailable. Refresh and try again.");
        return mode;
    }

    private static string Identity(string device)
    {
        var item = new Native.DisplayDevice { Size = Marshal.SizeOf<Native.DisplayDevice>() };
        if (!Native.EnumDisplayDevices(device, 0, ref item, 1))
            throw new InvalidOperationException("Display disconnected. Refresh and try again.");
        return string.IsNullOrWhiteSpace(item.Id) ? device : item.Id;
    }

    internal static DisplayMode Describe(Native.DevMode mode) => new(mode.Width, mode.Height, mode.Frequency);
    internal static bool Eligible(Native.DevMode mode, Native.DevMode current) =>
        mode.Width > 0 && mode.Height > 0 && mode.Frequency > 1 &&
        mode.BitsPerPel == current.BitsPerPel && mode.Orientation == current.Orientation &&
        mode.DisplayFlags == current.DisplayFlags;

    private static List<Native.DevMode> Enumerate(string device, Native.DevMode current)
    {
        var result = new List<Native.DevMode>();
        for (int index = 0; ; index++)
        {
            var mode = new Native.DevMode { Size = (ushort)Marshal.SizeOf<Native.DevMode>() };
            if (!Native.EnumDisplaySettings(device, index, ref mode)) break;
            if (Eligible(mode, current)) result.Add(mode);
        }
        return result;
    }

    internal static List<DisplayMode> Read(string device) => Enumerate(device, Current(device))
        .Select(Describe).Distinct().OrderByDescending(m => m.Width).ThenByDescending(m => m.Height)
        .ThenByDescending(m => m.Hertz).ToList();

    internal static ModePreview Apply(Display display, DisplayMode selected)
    {
        if (Identity(display.Device) != display.Id)
            throw new InvalidOperationException("Display configuration changed. Refresh and try again.");
        var original = Current(display.Device);
        if (Describe(original) != new DisplayMode((uint)display.Width, (uint)display.Height, display.Hertz))
            throw new InvalidOperationException("Display settings changed elsewhere. Refresh and try again.");
        var candidates = Enumerate(display.Device, original);
        if (!candidates.Any(m => Describe(m) == selected))
            throw new InvalidOperationException("This mode is no longer supported. Refresh and try again.");
        var target = candidates.First(m => Describe(m) == selected);
        // Only change the requested mode, preserving desktop position and orientation.
        target.Fields = 0x00040000 | 0x00080000 | 0x00100000 | 0x00200000 | 0x00400000;
        EnsureSuccess(Native.ChangeDisplaySettingsEx(display.Device, ref target, 0, 2, 0));
        original.Fields = target.Fields;
        var preview = new ModePreview(() =>
        {
            if (Identity(display.Device) != display.Id)
                throw new InvalidOperationException("Original display is disconnected. Restore its mode in Windows Settings after reconnecting.");
            EnsureSuccess(Native.ChangeDisplaySettingsEx(display.Device, ref original, 0, 0, 0));
            if (Describe(Current(display.Device)) != Describe(original))
                throw new InvalidOperationException("Windows did not restore the previous mode. Open Windows Display Settings to recover.");
        });
        try
        {
            EnsureSuccess(Native.ChangeDisplaySettingsEx(display.Device, ref target, 0, 0, 0));
            if (Describe(Current(display.Device)) != selected)
                throw new InvalidOperationException("Windows did not apply the requested mode.");
            preview.Start();
            return preview;
        }
        catch (Exception ex)
        {
            preview.Revert();
            throw new InvalidOperationException(ex.Message + " " + preview.Completion.Task.GetAwaiter().GetResult(), ex);
        }
    }

    internal static void EnsureSuccess(int code)
    {
        if (code != 0) throw new InvalidOperationException(code switch
        {
            1 => "This mode requires a Windows restart and cannot be previewed.",
            -2 => "Windows rejected this display mode.",
            _ => $"Windows could not change the display mode (code {code})."
        });
    }
}

// The rollback timer runs independently of the WPF dispatcher.
internal sealed class ModePreview(Action restore) : IDisposable
{
    private readonly object sync = new();
    private System.Threading.Timer? timer;
    private bool finished;
    private long expires;
    internal DateTime Deadline { get; private set; }
    internal TaskCompletionSource<string> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal void Start(TimeSpan? duration = null)
    {
        lock (sync)
        {
            var timeout = duration ?? TimeSpan.FromSeconds(15);
            Deadline = DateTime.UtcNow.Add(timeout);
            expires = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
            timer = new System.Threading.Timer(_ => Revert(), null, timeout, Timeout.InfiniteTimeSpan);
        }
    }
    internal bool Keep()
    {
        lock (sync)
        {
            if (finished) return false;
            if (Environment.TickCount64 >= expires) { Revert(); return false; }
            finished = true; timer?.Dispose();
            Completion.TrySetResult("Display mode kept for this Windows session.");
            return true;
        }
    }
    internal void Revert()
    {
        lock (sync)
        {
            if (finished) return;
            finished = true; timer?.Dispose();
            try { restore(); Completion.TrySetResult("Previous display mode restored."); }
            catch (Exception ex) { Completion.TrySetResult("Could not restore display mode: " + ex.Message); }
        }
    }
    public void Dispose() => Revert();
}
