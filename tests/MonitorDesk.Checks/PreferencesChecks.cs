using System.IO;
using System.Text.Json;
using System.Windows;
using MonitorDesk.Services;

internal static class PreferencesChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MonitorDesk-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = System.IO.Path.Combine(dir, "settings.json");
        try
        {
            var store = new PreferencesStore(path);
            check(store.Load() == new Preferences(), "Missing preferences use defaults without creating a file");
            var saved = new Preferences(true, 67, new(-1280, 25, 950, 750), true);
            store.Save(saved);
            check(store.Load() == saved, "Theme, eye intensity, window bounds and maximized state survive a settings round trip");
            store.Save(saved with { EyeStrength = 100 });
            check(store.Load().EyeStrength == 90 && Directory.GetFiles(dir).Length == 1, "Atomic preference replacement validates intensity and leaves no temporary files");
            File.WriteAllText(path, "{}");
            check(store.Load() == new Preferences(), "Missing JSON fields keep sensible preference defaults");
            File.WriteAllText(path, "broken JSON");
            try { store.Load(); throw new Exception("Corrupt preferences accepted"); }
            catch (JsonException) { check(File.ReadAllText(path) == "broken JSON", "Corrupt preferences are reported without changing the file"); }
            var invalid = new Preferences(false, -5, new(999999, -999999, 1, 99999)).Validated();
            check(invalid.EyeStrength == 0 && invalid.Window is { Left: 200000, Top: -200000, Width: 680, Height: 4000 }, "Invalid saved placement and intensity are bounded");
            check(new Preferences(Window: new(0, 0, double.NaN, 700)).Validated().Window == null, "Non-finite saved bounds are discarded");
        }
        finally
        {
            foreach (string file in Directory.GetFiles(dir)) File.Delete(file);
            Directory.Delete(dir);
        }
        Rect[] screens = [new(-1920, 0, 1920, 1040), new(0, 0, 1920, 1040)];
        var negative = new Rect(-1800, 100, 900, 700);
        check(PlacementGeometry.Fit(negative, screens) == negative, "Valid negative monitor coordinates are preserved");
        var recovered = PlacementGeometry.Fit(new(4000, 3000, 1200, 900), screens);
        check(screens[1].Contains(recovered), "Disconnected-monitor placement is recovered inside a connected work area");
        var oversized = PlacementGeometry.Fit(new(100, 100, 3000, 2000), screens);
        check(screens.Any(s => s.Contains(oversized)), "Oversized windows are fitted above the taskbar");
        Rect[] gap = [new(0, 0, 1000, 1000), new(3000, 0, 1000, 1000)];
        check(gap.Any(s => s.Contains(PlacementGeometry.Fit(new(1700, 0, 700, 600), gap))), "Saved positions in monitor-layout gaps are recovered onto a screen");

        string? command = null;
        int writes = 0;
        var startup = new StartupRegistration(@"C:\Users\Test User\Uygulamalar\MonitorDesk.exe", () => command,
            value => { command = value; writes++; });
        check(!startup.Enabled && writes == 0, "Reading startup preference never enables it");
        startup.SetEnabled(true);
        check(startup.Enabled && command == "\"C:\\Users\\Test User\\Uygulamalar\\MonitorDesk.exe\" --startup --tray", "Startup command quotes the executable and requests silent tray launch");
        startup.SetEnabled(false);
        check(!startup.Enabled && command == null, "Disabling startup removes the managed entry");
        command = "another application's startup command"; writes = 0;
        startup.SetEnabled(false);
        check(command != null && writes == 0, "Disabling startup preserves an unrelated entry");
        try { _ = new StartupRegistration("relative.exe", () => null, _ => { }); throw new Exception("Relative startup path accepted"); }
        catch (ArgumentException) { check(true, "Startup requires a fully qualified executable path"); }
    }
}
