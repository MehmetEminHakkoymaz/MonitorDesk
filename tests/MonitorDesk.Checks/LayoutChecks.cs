using MonitorDesk.Services;
using System.Runtime.InteropServices;

internal static class LayoutChecks
{
    internal static async Task Run(Action<bool, string> check, bool hardwareRead)
    {
        check(Marshal.SizeOf<LayoutNative.Path>() == 72 && Marshal.SizeOf<LayoutNative.Mode>() == 64 && Marshal.SizeOf<LayoutNative.SourceName>() == 84, "CCD native structures match Windows ABI");
        check(Marshal.OffsetOf<LayoutNative.Mode>(nameof(LayoutNative.Mode.X)).ToInt32() == 28 && Marshal.OffsetOf<LayoutNative.Mode>(nameof(LayoutNative.Mode.Y)).ToInt32() == 32, "CCD position offsets match source-mode ABI");
        var a = new ScreenPlacement("A", 1, true, 0, 0, 1920, 1080);
        var b = new ScreenPlacement("B", 2, false, 1920, 0, 1080, 1920);
        check(LayoutGeometry.Validate([a, b]) == null, "Mixed-size landscape and portrait displays can share an edge");
        check(LayoutGeometry.Validate([a, b with { X = -1080, Y = -200 }]) == null, "Negative desktop coordinates are valid");
        check(LayoutGeometry.Validate([a, b with { X = 1900 }]) != null, "Overlapping screens are rejected");
        check(LayoutGeometry.Validate([a, b with { X = 1921 }]) != null, "Disconnected one-pixel gap is rejected");
        check(LayoutGeometry.Validate([a, b with { Y = 1080 }]) != null, "Corner-only contact is rejected");
        var c = new ScreenPlacement("C", 3, false, 3000, 0, 1280, 720);
        check(LayoutGeometry.Validate([a, b, c]) == null, "Connected chains do not require every screen to touch the primary");
        var normalized = LayoutGeometry.Normalize([a with { X = 400, Y = -50 }, b with { X = 2320, Y = -50 }]);
        check(normalized.SequenceEqual(new[] { a, b }), "Moving primary normalizes coordinates without changing relative layout");
        check(LayoutGeometry.Snap(b with { X = 1930, Y = 8 }, [a], 15) == b, "Dragging snaps edges and aligned tops");
        check(LayoutGeometry.Snap(b with { X = 2100 }, [a], 15).X == 2100, "Distant edges do not snap");

        var backend = new FakeBackend();
        var portraitSnapshot = backend.Read(); portraitSnapshot.Paths[1].Rotation = 2;
        check(portraitSnapshot.Screens()[1].Width == 1080 && portraitSnapshot.Screens()[1].Height == 1920, "CCD quarter-turn rotation is reflected in desktop tile bounds without changing source dimensions");
        var service = new DisplayLayout(backend);
        var original = backend.Read();
        var requested = original.Screens().Select(s => s.Device == "B" ? s with { X = -1920 } : s).ToList();
        using (var preview = service.Apply(original, requested))
        {
            check(backend.Calls.SequenceEqual(new[] { true, false }), "Layout validates before one batch apply");
            check(backend.State.Modes[1].X == -1920 && original.Modes[1].X == 1920, "Only copied source position changes");
            check(backend.State.Paths.SequenceEqual(original.Paths) && backend.State.Modes[2].Equals(original.Modes[2]), "Layout preserves path timing and full target-mode payload");
            preview.Revert(); await preview.Completion.Task;
            check(DisplayLayout.SameState(original, backend.State), "Explicit rollback restores all original positions");
        }
        backend = new FakeBackend(); service = new(backend); original = backend.Read();
        using (var preview = service.Apply(original, requested))
        {
            check(preview.Keep(), "Layout preview can be confirmed");
            preview.Dispose();
            check(backend.State.Modes[1].X == -1920 && backend.Calls.Count == 2, "Confirmed layout is not reverted on close");
        }
        backend = new FakeBackend { RejectTest = true }; service = new(backend);
        try { service.Apply(backend.Read(), requested); throw new Exception("Validation failure accepted"); }
        catch (InvalidOperationException) { check(backend.Calls.SequenceEqual(new[] { true }), "Rejected layout never makes a display write"); }
        backend = new FakeBackend { FailNextApply = true }; service = new(backend); original = backend.Read();
        try { service.Apply(original, requested); throw new Exception("Apply failure accepted"); }
        catch (InvalidOperationException) { check(DisplayLayout.SameState(original, backend.State) && backend.Calls.Count == 3, "Failed batch apply triggers rollback"); }
        backend = new FakeBackend(); service = new(backend); original = backend.Read();
        backend.State.Modes[1].X = 2000;
        try { service.Apply(original, requested); throw new Exception("Stale layout accepted"); }
        catch (InvalidOperationException) { check(backend.Calls.Count == 0, "External position changes reject stale drafts before native calls"); }
        backend = new FakeBackend(); service = new(backend);
        using (var preview = service.Apply(backend.Read(), requested))
        {
            backend.State.Paths[1].TargetId = 99;
            preview.Revert();
            check((await preview.Completion.Task).Contains("Connected displays changed") && backend.Calls.Count == 2, "Rollback does not apply an old topology to changed connections");
        }
        var clone = new FakeBackend().Read(); clone.Devices[1] = "A";
        try { clone.Screens(); throw new Exception("Clone accepted"); }
        catch (InvalidOperationException) { check(true, "Mirrored sources are rejected with guidance to use Extend"); }
        if (hardwareRead)
        {
            var native = new WindowsLayoutBackend(); var snapshot = native.Read();
            var screens = snapshot.Screens();
            check(screens.Count > 0 && screens.All(s => s.Width > 0 && s.Height > 0), "Real CCD layout can be read without changing monitors");
            check(screens.All(s => { var current = DisplayModes.Current(s.Device); return s.Width == current.Width && s.Height == current.Height && s.X == current.X && s.Y == current.Y; }), "CCD desktop bounds agree with Windows display settings including portrait screens");
            try
            {
                native.Set(snapshot, true);
                check(true, "Windows validates the unchanged CCD layout without applying it");
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5)
            {
                Console.WriteLine("SKIP native layout validation: this process cannot access the interactive display session (Windows error 5). Run from the user's desktop to validate.");
            }
            foreach (var path in snapshot.Paths) Console.WriteLine($"CCD rotation={path.Rotation} sourceIndex={path.SourceMode} size={snapshot.Modes[path.SourceMode].Width}x{snapshot.Modes[path.SourceMode].Height}");
            foreach (var screen in screens) Console.WriteLine($"Layout: {screen.Device} at {screen.X},{screen.Y}, {screen.Width}x{screen.Height}");
        }
    }

    private sealed class FakeBackend : ILayoutBackend
    {
        internal LayoutSnapshot State = new(
            [new() { SourceId = 0, TargetId = 10, SourceMode = 0, TargetMode = 2, RateNumerator = 60000, RateDenominator = 1001, Flags = 1 },
             new() { SourceId = 1, TargetId = 11, SourceMode = 1, TargetMode = 3, RateNumerator = 60000, RateDenominator = 1001, Flags = 1 }],
            [new() { Type = 1, Id = 0, Width = 1920, Height = 1080 }, new() { Type = 1, Id = 1, Width = 1920, Height = 1080, X = 1920 },
             new() { Type = 2, Id = 10, Data0 = 123456, Data5 = 987654 }, new() { Type = 2, Id = 11, Data0 = 99999 }], ["A", "B"]);
        internal List<bool> Calls = [];
        internal bool RejectTest, FailNextApply;
        public LayoutSnapshot Read() => State.Copy();
        public void Set(LayoutSnapshot snapshot, bool testOnly)
        {
            Calls.Add(testOnly);
            if (testOnly && RejectTest) throw new InvalidOperationException("Driver rejected layout");
            if (testOnly) return;
            State = snapshot.Copy();
            if (FailNextApply) { FailNextApply = false; throw new InvalidOperationException("Driver apply failed"); }
        }
    }
}
