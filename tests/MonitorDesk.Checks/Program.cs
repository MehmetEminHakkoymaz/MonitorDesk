using MonitorDesk.Services;
using System.Reflection;
using System.Runtime.InteropServices;

if (args.Contains("--diagnose"))
{
    for (int round = 1; round <= 8; round++)
    {
        Native.MonitorCallback callback = (nint monitor, nint dc, ref Native.Rect rect, nint data) =>
        {
            var info = new Native.MonitorInfo { Size = Marshal.SizeOf<Native.MonitorInfo>() };
            if (!Native.GetMonitorInfo(monitor, ref info) || !Native.GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out uint number)) return true;
            var physical = new Native.Physical[number];
            if (!Native.GetPhysicalMonitorsFromHMONITOR(monitor, number, physical)) return true;
            try
            {
                foreach (var p in physical)
                {
                    bool b = Native.GetMonitorBrightness(p.Handle, out uint bmin, out uint bv, out uint bmax);
                    int be = b ? 0 : Marshal.GetLastWin32Error();
                    if (args.Contains("--paced")) Thread.Sleep(150);
                    bool c = Native.GetMonitorContrast(p.Handle, out uint cmin, out uint cv, out uint cmax);
                    int ce = c ? 0 : Marshal.GetLastWin32Error();
                    Console.WriteLine($"round={round} device={info.Device} brightness={b}:{bv}/{bmax} error={be} contrast={c}:{cv}/{cmax} error={ce}");
                }
            }
            finally { Native.DestroyPhysicalMonitors(number, physical); }
            return true;
        };
        Native.EnumDisplayMonitors(0, 0, callback, 0);
        await Task.Delay(400);
    }
    return;
}
if (args.Contains("--service-diagnose"))
{
    var monitored = new DisplayService();
    for (int round = 1; round <= 8; round++)
    {
        foreach (var screen in await monitored.ReadAsync())
            Console.WriteLine($"round={round} display={screen.Number} contrast={screen.Contrast?.Current} stale={screen.Contrast?.IsStale}");
        await Task.Delay(400);
    }
    return;
}
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



var delays = new List<int>();
var reader = new LevelReader(delays.Add);
int attempts = 0;
int transportError = unchecked((int)0xC0262582);
var recovered = reader.Read("display2|contrast", () => ++attempts < 3 ? new(null, transportError) : new(new Level(0, 75, 100), 0));
Check(attempts == 3 && recovered?.Current == 75 && !recovered.IsStale, "Transient transport failures recover within three attempts");
Check(delays.SequenceEqual(new[] {150, 350, 700}), "DDC retries allow increasing firmware recovery time");
attempts = 0;
var stale = reader.Read("display2|contrast", () => { attempts++; return new(null, transportError); });
Check(attempts == 3 && stale is { Current: 75, IsStale: true }, "Exhausted retries retain an explicitly stale last-known value");
Check(reader.Read("other|contrast", () => new(null, transportError)) == null, "Unknown controls never receive invented cached values");
attempts = 0;
reader.Read("unsupported", () => { attempts++; return new(null, unchecked((int)0xC0262584)); });
Check(attempts == 1, "Explicit unsupported errors are not retried");
var fresh = reader.Read("display2|contrast", () => new(new Level(0, 70, 100), 0));
Check(fresh is { Current: 70, IsStale: false }, "A successful refresh replaces stale data");
reader.Retain([]);
Check(reader.Unavailable("display2|contrast") == null, "Disconnect removes cached display values");
try
{
    await service.SetAsync(display with { Contrast = new Level(0, 75, 100, true) }, true, 70);
    throw new Exception("Stale data allowed a write");
}
catch (ArgumentOutOfRangeException) { Check(true, "Stale controls reject writes before hardware access"); }
Console.WriteLine($"Total after DDC regression tests: {count} checks passed.");

var writeDelays = new List<int>();
var writer = new DdcWriter(writeDelays.Add);
int writeAttempts = 0;
writer.Write(() => ++writeAttempts < 3 ? transportError : 0);
Check(writeAttempts == 3 && writeDelays.SequenceEqual(new[] { 200, 500, 1000, 350 }), "Transient writes recover with bounded retries and settle before read-back");
writeAttempts = 0;
try { writer.Write(() => { writeAttempts++; return transportError; }); throw new Exception("Failed writes reported success"); }
catch (System.ComponentModel.Win32Exception ex) { Check(writeAttempts == 3 && ex.NativeErrorCode == transportError, "Exhausted writes preserve the native error and stop after three attempts"); }
writeAttempts = 0;
try { writer.Write(() => { writeAttempts++; return unchecked((int)0xC0262584); }); throw new Exception("Unsupported write accepted"); }
catch (System.ComponentModel.Win32Exception) { Check(writeAttempts == 1, "Unsupported writes are not retried"); }
writeAttempts = 0;
writer.Write(() => { writeAttempts++; return 0; });
Check(writeAttempts == 1, "Successful writes are not repeated");

var currentMode = new Native.DevMode { Width = 1920, Height = 1080, Frequency = 60, BitsPerPel = 32 };
Check(DisplayModes.Eligible(currentMode, currentMode), "Current progressive mode is eligible");
var invalidMode = currentMode; invalidMode.Frequency = 1;
Check(!DisplayModes.Eligible(invalidMode, currentMode), "Default or unknown refresh rates are excluded");
invalidMode = currentMode; invalidMode.Orientation = 1;
Check(!DisplayModes.Eligible(invalidMode, currentMode), "Mode choices preserve orientation");
invalidMode = currentMode; invalidMode.BitsPerPel = 16;
Check(!DisplayModes.Eligible(invalidMode, currentMode), "Mode choices preserve color depth");
invalidMode = currentMode; invalidMode.DisplayFlags = 2;
Check(!DisplayModes.Eligible(invalidMode, currentMode), "Mode choices preserve scan type");
foreach (int code in new[] { 1, -1, -2, -3, -4, -5, -6 })
{
    try { DisplayModes.EnsureSuccess(code); throw new Exception("Driver failure was accepted"); }
    catch (InvalidOperationException) { Check(true, $"Display mode result {code} is not reported as success"); }
}
int restored = 0;
using (var preview = new ModePreview(() => Interlocked.Increment(ref restored)))
{
    preview.Start(TimeSpan.FromMilliseconds(50));
    await preview.Completion.Task.WaitAsync(TimeSpan.FromSeconds(3));
    Check(restored == 1 && !preview.Keep(), "Timeout restores once and rejects late confirmation");
    preview.Revert();
    Check(restored == 1, "Repeated revert is idempotent");
}
restored = 0;
using (var preview = new ModePreview(() => restored++))
{
    preview.Start();
    Check(preview.Keep(), "Explicit confirmation keeps an active preview");
    preview.Revert();
    Check(restored == 0, "Confirmed preview is not reverted on disposal or close");
}
using (var preview = new ModePreview(() => throw new InvalidOperationException("Disconnected")))
{
    preview.Start(); preview.Revert();
    Check((await preview.Completion.Task).Contains("Disconnected"), "Rollback failures are reported to the user");
}
restored = 0;
var disposedPreview = new ModePreview(() => restored++);
disposedPreview.Start(); disposedPreview.Dispose();
Check(restored == 1, "Closing an unconfirmed preview restores the original mode");
for (int attempt = 0; attempt < 25; attempt++)
{
    int rollbacks = 0;
    using var preview = new ModePreview(() => Interlocked.Increment(ref rollbacks));
    preview.Start();
    bool kept = false;
    await Task.WhenAll(Task.Run(() => kept = preview.Keep()), Task.Run(preview.Revert));
    if (rollbacks != (kept ? 0 : 1)) throw new Exception("Confirmation and rollback race");
}
Check(true, "Concurrent confirmation and rollback have exactly one outcome");
if (args.Contains("--hardware-read"))
{
    foreach (var screen in (await service.ReadAsync()).DistinctBy(d => d.Device))
    {
        var modes = DisplayModes.Read(screen.Device);
        Check(modes.Count > 0, $"Display {screen.Number} exposes {modes.Count} compatible modes without writes");
    }
}
Console.WriteLine($"Final total: {count} checks passed. No display settings were changed.");

for (uint from = 0; from < 4; from++)
{
    var source = DisplayModes.Rotate(currentMode, from);
    for (uint to = 0; to < 4; to++)
    {
        var target = DisplayModes.Rotate(source, to);
        Check(target.Orientation == to && target.Width == (to % 2 == 0 ? 1920u : 1080u) && target.Height == (to % 2 == 0 ? 1080u : 1920u)
            && target.Frequency == 60 && target.BitsPerPel == 32 && (target.Fields & 0x80) != 0,
            $"Orientation {from} to {to} preserves timing and sets correct dimensions and native field");
        Check(DisplayModes.Describe(DisplayModes.Rotate(target, from)) == DisplayModes.Describe(source),
            $"Orientation {from} to {to} round trip restores the full mode");
    }
}
string[] expectedNames = ["Landscape", "Portrait", "Landscape (flipped)", "Portrait (flipped)"];
for (uint i = 0; i < 4; i++)
    Check(DisplayModes.Describe(DisplayModes.Rotate(currentMode, i)).OrientationName == expectedNames[i], $"Orientation {i} has the expected label");
var nativePortrait = currentMode; nativePortrait.Width = 1080; nativePortrait.Height = 1920;
Check(DisplayModes.Describe(nativePortrait).OrientationName == "Portrait" && DisplayModes.Describe(DisplayModes.Rotate(nativePortrait, 3)).OrientationName == "Landscape", "Native portrait panels use correct orientation labels");
Check(DisplayModes.Describe(currentMode) != DisplayModes.Describe(DisplayModes.Rotate(currentMode, 2)), "Read-back detects flipped orientation even when pixel dimensions are unchanged");
try { DisplayModes.Rotate(currentMode, 4); throw new Exception("Invalid orientation accepted"); }
catch (ArgumentOutOfRangeException) { Check(true, "Invalid orientation is rejected before native access"); }
Console.WriteLine($"Total including orientation: {count} checks passed. No display settings were changed.");
await LayoutChecks.Run(Check, args.Contains("--hardware-read"));
Console.WriteLine($"Total including layout: {count} checks passed. No display settings were changed.");
await ProfileChecks.Run(Check);
Console.WriteLine($"Total including profiles: {count} checks passed. No display settings were changed.");
LocalizationChecks.Run(Check);
Console.WriteLine($"Total including localization: {count} checks passed. No display settings were changed.");
LevelChangeChecks.Run(Check);
Console.WriteLine($"Total including automatic sliders: {count} checks passed. No display settings were changed.");
PreferencesChecks.Run(Check);
RecoveryChecks.Run(Check);
Console.WriteLine($"Total including preferences: {count} checks passed. No real settings or startup entries were changed.");
