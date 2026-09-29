using System.Runtime.InteropServices;
using System.Collections;
using Microsoft.CSharp.RuntimeBinder;

namespace MonitorDesk.Services;
public record Level(uint Min, uint Current, uint Max, bool IsStale = false);
public record Display(string Id, string Device, string Name, int Number, bool Primary,
    int Left, int Top, int Width, int Height, uint Hertz, int PhysicalIndex,
    Level? Brightness, Level? Contrast, string? WmiInstance, string Note);

internal static class WmiBrightness
{
    internal record Panel(string Instance, byte Value, byte[] Steps);
    private static void Release(object? value) { if (value != null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
    // WMI is optional. Desktop PCs can expose missing or incomplete brightness providers.
    internal static List<Panel> ReadSafely(Func<List<Panel>> provider)
    {
        try { return provider() ?? []; }
        catch (Exception ex) when (ex is COMException or RuntimeBinderException or InvalidCastException or InvalidOperationException)
        {
            System.Diagnostics.Trace.TraceWarning("Optional WMI brightness provider unavailable: {0}", ex);
            return [];
        }
    }
    internal static Panel? ParsePanel(object? instance, object? brightness, object? levels)
    {
        if (instance is not string name || string.IsNullOrWhiteSpace(name) || brightness == null) return null;
        try
        {
            int current = Convert.ToInt32(brightness);
            if (current < 0 || current > 100) return null;
            var steps = levels is Array array ? array.Cast<object>().Select(Convert.ToInt32).Where(x => x >= 0 && x <= 100).Select(x => (byte)x).Distinct().ToArray() : [];
            return new Panel(name, (byte)current, steps);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return null; }
    }
    internal static List<Panel> Read() => ReadSafely(ReadCore);
    private static List<Panel> ReadCore()
    {
        var result = new List<Panel>();
        object? locator = null, service = null, rows = null;
        try
        {
            Type? type = Type.GetTypeFromProgID("WbemScripting.SWbemLocator");
            if (type == null) return result;
            locator = Activator.CreateInstance(type);
            if (locator == null) return result;
            service = ((dynamic)locator).ConnectServer(".", "root\\wmi");
            if (service == null) return result;
            rows = ((dynamic)service).ExecQuery("SELECT * FROM WmiMonitorBrightness WHERE Active = TRUE");
            if (rows is not IEnumerable enumerable) return result;
            foreach (object? row in enumerable)
            {
                if (row == null) continue;
                try
                {
                    dynamic item = row;
                    // Keep null COM values out of dynamic extension-method dispatch.
                    object? instance = item.InstanceName;
                    object? brightness = item.CurrentBrightness;
                    object? levels = item.Level;
                    Panel? panel = ParsePanel(instance, brightness, levels);
                    if (panel != null) result.Add(panel);
                }
                catch (Exception ex) when (ex is COMException or RuntimeBinderException or InvalidCastException)
                { System.Diagnostics.Trace.TraceWarning("Skipping incomplete WMI brightness record: {0}", ex.Message); }
                finally { Release(row); }
            }
        }
        finally { Release(rows); Release(service); Release(locator); }
        return result;
    }
    internal static void Set(string instance, uint value)
    {
        object? locator = null, service = null, rows = null;
        try
        {
            locator = Activator.CreateInstance(Type.GetTypeFromProgID("WbemScripting.SWbemLocator")!);
            service = ((dynamic)locator!).ConnectServer(".", "root\\wmi");
            rows = ((dynamic)service).ExecQuery("SELECT * FROM WmiMonitorBrightnessMethods WHERE Active = TRUE");
            foreach (object row in (dynamic)rows)
            {
                try
                {
                    dynamic item = row;
                    if (!string.Equals((string)item.InstanceName, instance, StringComparison.OrdinalIgnoreCase)) continue;
                    uint status = (uint)item.WmiSetBrightness(0u, (byte)value);
                    if (status != 0) throw new InvalidOperationException($"Windows rejected the brightness change (code {status}).");
                    return;
                }
                finally { Release(row); }
            }
            throw new InvalidOperationException("The display is no longer available. Refresh and try again.");
        }
        finally { Release(rows); Release(service); Release(locator); }
    }
}

public sealed class DisplayService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly LevelReader levelReader = new();
    private readonly Func<List<WmiBrightness.Panel>> readPanels;
    public DisplayService() : this(WmiBrightness.Read) { }
    internal DisplayService(Func<List<WmiBrightness.Panel>> readPanels) => this.readPanels = readPanels;
    public async Task<List<Display>> ReadAsync()
    {
        await gate.WaitAsync();
        try { return await Task.Run(Read); } finally { gate.Release(); }
    }
    private static List<(nint Handle, Native.MonitorInfo Info)> Logical()
    {
        var result = new List<(nint, Native.MonitorInfo)>();
        Native.MonitorCallback callback = (nint handle, nint dc, ref Native.Rect rect, nint data) =>
        {
            var info = new Native.MonitorInfo { Size = Marshal.SizeOf<Native.MonitorInfo>() };
            if (Native.GetMonitorInfo(handle, ref info)) result.Add((handle, info));
            return true;
        };
        if (!Native.EnumDisplayMonitors(0, 0, callback, 0)) throw new InvalidOperationException("Windows could not enumerate displays.");
        return result;
    }
    private static string Identity(string device)
    {
        var item = new Native.DisplayDevice { Size = Marshal.SizeOf<Native.DisplayDevice>() };
        if (!Native.EnumDisplayDevices(device, 0, ref item, 1)) return device;
        return string.IsNullOrWhiteSpace(item.Id) ? device : item.Id;
    }
    private static string WmiKey(string id)
    {
        var parts = id.Split('#');
        return parts.Length >= 3 ? $"DISPLAY\\{parts[1]}\\{parts[2]}" : id;
    }
    private static Native.Physical[] Physical(nint handle)
    {
        if (!Native.GetNumberOfPhysicalMonitorsFromHMONITOR(handle, out uint count) || count == 0) return [];
        var result = new Native.Physical[count];
        return Native.GetPhysicalMonitorsFromHMONITOR(handle, count, result) ? result : [];
    }
    private static LevelReply ReadLevel(nint handle, bool contrast)
    {
        uint min, current, max;
        bool ok = contrast ? Native.GetMonitorContrast(handle, out min, out current, out max)
                           : Native.GetMonitorBrightness(handle, out min, out current, out max);
        int error = ok ? 0 : Marshal.GetLastWin32Error();
        return ok && max > min && current >= min && current <= max ? new(new Level(min, current, max), 0) : new(null, ok ? unchecked((int)0xC0262585) : error);
    }
    private List<Display> Read()
    {
        var panels = WmiBrightness.ReadSafely(readPanels);
        var result = new List<Display>();
        var connected = new HashSet<string>(StringComparer.Ordinal);
        int number = 0;
        foreach (var (handle, info) in Logical().OrderBy(x => x.Info.Device, StringComparer.Ordinal))
        {
            number++;
            string id = Identity(info.Device);
            string key = WmiKey(id);
            var panel = panels.FirstOrDefault(p => p.Instance.Equals(key, StringComparison.OrdinalIgnoreCase) || p.Instance.StartsWith(key + "_", StringComparison.OrdinalIgnoreCase));
            var mode = new Native.DevMode { Size = (ushort)Marshal.SizeOf<Native.DevMode>() };
            bool hasMode = Native.EnumDisplaySettings(info.Device, -1, ref mode);
            var physical = Physical(handle);
            try
            {
                int count = Math.Max(1, physical.Length);
                for (int index = 0; index < count; index++)
                {
                    string controlKey = $"{id}|{index}";
                    string brightnessKey = controlKey + "|brightness", contrastKey = controlKey + "|contrast";
                    connected.Add(brightnessKey); connected.Add(contrastKey);
                    var brightness = panel != null ? new Level(0, panel.Value, 100) : physical.Length > 0
                        ? levelReader.Read(brightnessKey, () => ReadLevel(physical[index].Handle, false)) : levelReader.Unavailable(brightnessKey);
                    var contrast = physical.Length > 0
                        ? levelReader.Read(contrastKey, () => ReadLevel(physical[index].Handle, true)) : levelReader.Unavailable(contrastKey);
                    string name = physical.Length > 0 ? physical[index].Description : "Windows display";
                    string note = panel != null ? "Built-in brightness · Windows WMI" : brightness != null || contrast != null ? "Hardware controls · DDC/CI" : "Hardware controls unavailable. Check DDC/CI in the monitor menu and your cable or dock.";
                    result.Add(new Display(id, info.Device, name, number, (info.Flags & 1) != 0,
                        info.Monitor.Left, info.Monitor.Top, hasMode ? (int)mode.Width : info.Monitor.Right - info.Monitor.Left,
                        hasMode ? (int)mode.Height : info.Monitor.Bottom - info.Monitor.Top, hasMode ? mode.Frequency : 0,
                        index, brightness, contrast, panel?.Instance, note));
                }
            }
            finally { if (physical.Length > 0) Native.DestroyPhysicalMonitors((uint)physical.Length, physical); }
        }
        levelReader.Retain(connected);
        return result;
    }
    public async Task SetAsync(Display display, bool contrast, uint value)
    {
        var level = contrast ? display.Contrast : display.Brightness;
        if (level == null || level.IsStale || value < level.Min || value > level.Max) throw new ArgumentOutOfRangeException(nameof(value));
        await gate.WaitAsync();
        try
        {
            await Task.Run(() =>
            {
                if (!contrast && display.WmiInstance != null)
                {
                    var panel = WmiBrightness.Read().FirstOrDefault(p => p.Instance == display.WmiInstance)
                        ?? throw new InvalidOperationException("Display disconnected. Refresh and try again.");
                    uint nearest = panel.Steps.Length > 0 ? panel.Steps.OrderBy(s => Math.Abs((int)s - (int)value)).First() : value;
                    WmiBrightness.Set(display.WmiInstance, nearest);
                    return;
                }
                var target = Logical().FirstOrDefault(x => x.Info.Device == display.Device && Identity(x.Info.Device) == display.Id);
                if (target.Handle == 0) throw new InvalidOperationException("Display disconnected. Refresh and try again.");
                var physical = Physical(target.Handle);
                try
                {
                    if (display.PhysicalIndex >= physical.Length || physical[display.PhysicalIndex].Description != display.Name)
                        throw new InvalidOperationException("Display configuration changed. Refresh and try again.");
                    nint handle = physical[display.PhysicalIndex].Handle;
                    Thread.Sleep(150);
                    bool ok = contrast ? Native.SetMonitorContrast(handle, value) : Native.SetMonitorBrightness(handle, value);
                    if (!ok) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "The monitor did not accept this setting. Check its on-screen menu.");
                }
                finally { if (physical.Length > 0) Native.DestroyPhysicalMonitors((uint)physical.Length, physical); }
            });
        }
        finally { gate.Release(); }
    }
}
