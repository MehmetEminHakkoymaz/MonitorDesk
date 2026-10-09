using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MonitorDesk.Services;

namespace MonitorDesk;

// A click-through color overlay, not Windows Night light or a hardware color setting.
internal sealed class WarmFilter : IDisposable
{
    private readonly Dictionary<string, Window> windows = [];
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(nint window, int index, int value);

    internal void Show(IEnumerable<Display> displays, int strength)
    {
        byte opacity = (byte)Math.Clamp(strength, 0, 90);
        if (opacity == 0) { Dispose(); return; }
        var targets = displays.DistinctBy(d => d.Device).ToList();
        try
        {
            foreach (var disconnected in windows.Keys.Where(device => !targets.Any(d => d.Device == device)).ToArray())
            {
                windows[disconnected].Close(); windows.Remove(disconnected);
            }
            foreach (var display in targets)
            {
                if (!windows.TryGetValue(display.Device, out var window))
                {
                    window = new Window
                    {
                    WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                    AllowsTransparency = true, Background = new SolidColorBrush(Color.FromArgb(opacity, 255, 150, 35)),
                    ShowInTaskbar = false, ShowActivated = false, Topmost = true, Focusable = false,
                    IsHitTestVisible = false, Width = 1, Height = 1
                    };
                    window.SourceInitialized += (_, _) =>
                    {
                        nint handle = new WindowInteropHelper(window).Handle;
                        // WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW.
                        SetWindowLong(handle, -20, GetWindowLong(handle, -20) | 0x20 | 0x08000000 | 0x80);
                    };
                    windows.Add(display.Device, window);
                    window.Show();
                }
                window.Background = new SolidColorBrush(Color.FromArgb(opacity, 255, 150, 35));
                if (!Native.SetWindowPos(new WindowInteropHelper(window).Handle, -1, display.Left, display.Top, display.Width, display.Height, 0x0010))
                    throw new InvalidOperationException("Windows could not position the warm filter.");
            }
        }
        catch { Dispose(); throw; }
    }

    public void Dispose()
    {
        foreach (var window in windows.Values) window.Close();
        windows.Clear();
    }
}
