using MonitorDesk.Services;
using System.Reflection;
using System.Runtime.InteropServices;

int count = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message); count++;
}
// Incorrect layouts can corrupt memory at the Win32 boundary.
var assembly = typeof(DisplayService).Assembly;
Check(Marshal.SizeOf(assembly.GetType("MonitorDesk.Services.Native+DevMode")!) == 220, "DEVMODEW matches the Windows ABI");
Check(Marshal.SizeOf(assembly.GetType("MonitorDesk.Services.Native+MonitorInfo")!) == 104, "MONITORINFOEXW matches the Windows ABI");
Check(Marshal.SizeOf(assembly.GetType("MonitorDesk.Services.Native+DisplayDevice")!) == 840, "DISPLAY_DEVICEW matches the Windows ABI");
Check(Marshal.SizeOf(assembly.GetType("MonitorDesk.Services.Native+Physical")!) == IntPtr.Size + 256, "PHYSICAL_MONITOR matches the Windows ABI");
var service = new DisplayService();
var display = new Display("test", "test", "test", 1, false, 0, 0, 1920, 1080, 60, 0, new Level(10, 50, 90), null, null, "test");
async Task Reject(bool contrast, uint value, string message)
{
    try { await service.SetAsync(display, contrast, value); throw new Exception("Invalid setting was accepted"); }
    catch (ArgumentOutOfRangeException) { Check(true, message); }
}
await Reject(false, 0, "Brightness below the monitor minimum is rejected before hardware access");
await Reject(false, 100, "Brightness above the monitor maximum is rejected before hardware access");
await Reject(true, 50, "Unsupported contrast is rejected before hardware access");
Console.WriteLine($"{count} checks passed. No display settings were changed.");

Check(WmiBrightness.ParsePanel(null, 50, null) == null, "Missing WMI identity is ignored");
Check(WmiBrightness.ParsePanel("panel", null, null) == null, "Missing WMI brightness is ignored");
Check(WmiBrightness.ParsePanel("panel", 101, null) == null, "Invalid WMI brightness is ignored");
Check(WmiBrightness.ParsePanel("panel", 50, null)?.Steps.Length == 0, "Null optional WMI level array is supported");
Check(WmiBrightness.ParsePanel("panel", (byte)50, new byte[] { 0, 50, 100 })?.Steps.Length == 3, "WMI byte arrays are parsed without dynamic extension dispatch");
Check(WmiBrightness.ReadSafely(() => throw new Microsoft.CSharp.RuntimeBinder.RuntimeBinderException("Cannot perform runtime binding on a null reference")).Count == 0, "Null COM binding failures do not block discovery");
if (args.Contains("--hardware-read"))
{
    var normal = await new DisplayService().ReadAsync();
    var failedWmi = await new DisplayService(() => throw new Microsoft.CSharp.RuntimeBinder.RuntimeBinderException("Cannot perform runtime binding on a null reference")).ReadAsync();
    Check(normal.Count > 0 && normal.Select(x => x.Id).SequenceEqual(failedWmi.Select(x => x.Id)), "Real displays remain discoverable when the WMI provider fails");
}
Console.WriteLine($"Total: {count} checks passed.");
