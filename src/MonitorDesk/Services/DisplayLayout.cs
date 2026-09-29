using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MonitorDesk.Services;

internal record ScreenPlacement(string Device, int Number, bool Primary, int X, int Y, int Width, int Height)
{
    internal long Right => (long)X + Width;
    internal long Bottom => (long)Y + Height;
}

internal static class LayoutGeometry
{
    internal static bool Overlaps(ScreenPlacement a, ScreenPlacement b) =>
        a.X < b.Right && a.Right > b.X && a.Y < b.Bottom && a.Bottom > b.Y;
    internal static bool Touches(ScreenPlacement a, ScreenPlacement b) =>
        ((a.Right == b.X || b.Right == a.X) && Math.Max(a.Y, b.Y) < Math.Min(a.Bottom, b.Bottom)) ||
        ((a.Bottom == b.Y || b.Bottom == a.Y) && Math.Max(a.X, b.X) < Math.Min(a.Right, b.Right));

    internal static List<ScreenPlacement> Normalize(IReadOnlyList<ScreenPlacement> screens)
    {
        var primary = screens.Single(s => s.Primary);
        return screens.Select(s => s with { X = checked(s.X - primary.X), Y = checked(s.Y - primary.Y) }).ToList();
    }

    internal static string? Validate(IReadOnlyList<ScreenPlacement> screens)
    {
        if (screens.Count < 2) return "Connect at least two extended displays to arrange them.";
        if (screens.Select(s => s.Device).Distinct().Count() != screens.Count || screens.Count(s => s.Primary) != 1)
            return "Display configuration changed. Close this window and refresh.";
        if (screens.Any(s => s.Width <= 0 || s.Height <= 0 || Math.Abs((long)s.X) > 100000 || Math.Abs((long)s.Y) > 100000))
            return "Display positions are outside the supported range.";
        for (int i = 0; i < screens.Count; i++)
            for (int j = i + 1; j < screens.Count; j++)
                if (Overlaps(screens[i], screens[j])) return "Screens overlap. Drag them apart until their edges meet.";
        var connected = new HashSet<string> { screens[0].Device };
        bool added;
        do
        {
            added = false;
            foreach (var screen in screens.Where(s => !connected.Contains(s.Device)))
                if (screens.Any(s => connected.Contains(s.Device) && Touches(s, screen))) added |= connected.Add(screen.Device);
        } while (added);
        return connected.Count == screens.Count ? null : "Leave no gaps: each screen must share an edge with the connected layout.";
    }

    internal static ScreenPlacement Snap(ScreenPlacement moving, IEnumerable<ScreenPlacement> others, int distance)
    {
        var screens = others.Where(s => s.Device != moving.Device).ToList();
        long bestX = distance + 1L, bestY = distance + 1L;
        int x = moving.X, y = moving.Y;
        foreach (var other in screens)
        {
            foreach (long candidate in new[] { other.X, other.Right, (long)other.X - moving.Width, other.Right - moving.Width })
                if (Math.Abs(candidate - moving.X) < bestX) { bestX = Math.Abs(candidate - moving.X); x = checked((int)candidate); }
            foreach (long candidate in new[] { other.Y, other.Bottom, (long)other.Y - moving.Height, other.Bottom - moving.Height })
                if (Math.Abs(candidate - moving.Y) < bestY) { bestY = Math.Abs(candidate - moving.Y); y = checked((int)candidate); }
        }
        return moving with { X = x, Y = y };
    }
}

internal static class LayoutNative
{
    // CCD structures retain the full source/target data; only source X/Y are edited.
    [StructLayout(LayoutKind.Explicit, Size = 72)]
    internal struct Path
    {
        [FieldOffset(0)] public long SourceAdapter;
        [FieldOffset(8)] public uint SourceId;
        [FieldOffset(12)] public uint SourceMode;
        [FieldOffset(16)] public uint SourceStatus;
        [FieldOffset(20)] public long TargetAdapter;
        [FieldOffset(28)] public uint TargetId;
        [FieldOffset(32)] public uint TargetMode;
        [FieldOffset(36)] public uint Technology;
        [FieldOffset(40)] public uint Rotation;
        [FieldOffset(44)] public uint Scaling;
        [FieldOffset(48)] public uint RateNumerator;
        [FieldOffset(52)] public uint RateDenominator;
        [FieldOffset(56)] public uint Scan;
        [FieldOffset(60)] public int Available;
        [FieldOffset(64)] public uint TargetStatus;
        [FieldOffset(68)] public uint Flags;
    }
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    internal struct Mode
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(4)] public uint Id;
        [FieldOffset(8)] public long Adapter;
        [FieldOffset(16)] public ulong Data0;
        [FieldOffset(24)] public ulong Data1;
        [FieldOffset(32)] public ulong Data2;
        [FieldOffset(40)] public ulong Data3;
        [FieldOffset(48)] public ulong Data4;
        [FieldOffset(56)] public ulong Data5;
        [FieldOffset(16)] public uint Width;
        [FieldOffset(20)] public uint Height;
        [FieldOffset(28)] public int X;
        [FieldOffset(32)] public int Y;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    internal struct SourceName
    {
        public uint Type, Size;
        public long Adapter;
        public uint Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
    }
    [DllImport("user32.dll")] internal static extern int GetDisplayConfigBufferSizes(uint flags, out uint paths, out uint modes);
    [DllImport("user32.dll")] internal static extern int QueryDisplayConfig(uint flags, ref uint paths, [Out] Path[] pathArray, ref uint modes, [Out] Mode[] modeArray, nint topology);
    [DllImport("user32.dll")] internal static extern int DisplayConfigGetDeviceInfo(ref SourceName name);
    [DllImport("user32.dll")] internal static extern int SetDisplayConfig(uint paths, [In] Path[] pathArray, uint modes, [In] Mode[] modeArray, uint flags);
}

internal sealed record LayoutSnapshot(LayoutNative.Path[] Paths, LayoutNative.Mode[] Modes, string[] Devices)
{
    internal LayoutSnapshot Copy() => new((LayoutNative.Path[])Paths.Clone(), (LayoutNative.Mode[])Modes.Clone(), (string[])Devices.Clone());
    internal List<ScreenPlacement> Screens()
    {
        var result = new List<ScreenPlacement>();
        for (int i = 0; i < Paths.Length; i++)
        {
            var path = Paths[i];
            if (path.SourceMode >= Modes.Length || Modes[path.SourceMode].Type != 1)
                throw new InvalidOperationException("Windows did not provide a usable source mode.");
            var mode = Modes[path.SourceMode];
            if (mode.Adapter != path.SourceAdapter || mode.Id != path.SourceId)
                throw new InvalidOperationException("Windows returned inconsistent display configuration data.");
            // CCD source dimensions are pre-rotation; desktop bounds include the path rotation.
            bool quarterTurn = path.Rotation is 2 or 4;
            result.Add(new(Devices[i], i + 1, mode.X == 0 && mode.Y == 0, mode.X, mode.Y,
                checked((int)(quarterTurn ? mode.Height : mode.Width)), checked((int)(quarterTurn ? mode.Width : mode.Height))));
        }
        if (result.Select(s => s.Device).Distinct().Count() != result.Count)
            throw new InvalidOperationException("Arrangement requires extended displays. Switch mirrored displays to Extend in Windows Settings.");
        return result;
    }
    internal string Connections => string.Join(";", Paths.Select(p => $"{p.SourceAdapter}:{p.SourceId}:{p.TargetAdapter}:{p.TargetId}").Order());
}

internal interface ILayoutBackend
{
    LayoutSnapshot Read();
    void Set(LayoutSnapshot snapshot, bool testOnly);
}

internal sealed class WindowsLayoutBackend : ILayoutBackend
{
    private static void Check(int error)
    {
        if (error != 0) throw new Win32Exception(error, error == 5
            ? "Windows denied access to the interactive display session. Open MonitorDesk directly from your Windows desktop and try again."
            : $"Windows display layout operation failed (code {error}).");
    }
    public LayoutSnapshot Read()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Check(LayoutNative.GetDisplayConfigBufferSizes(2, out uint pathCount, out uint modeCount));
            var paths = new LayoutNative.Path[pathCount]; var modes = new LayoutNative.Mode[modeCount];
            int error = LayoutNative.QueryDisplayConfig(2, ref pathCount, paths, ref modeCount, modes, 0);
            if (error == 122) continue; // Topology changed between size query and read.
            Check(error); Array.Resize(ref paths, (int)pathCount); Array.Resize(ref modes, (int)modeCount);
            var names = new string[pathCount];
            for (int i = 0; i < paths.Length; i++)
            {
                var name = new LayoutNative.SourceName { Type = 1, Size = (uint)Marshal.SizeOf<LayoutNative.SourceName>(), Adapter = paths[i].SourceAdapter, Id = paths[i].SourceId };
                Check(LayoutNative.DisplayConfigGetDeviceInfo(ref name)); names[i] = name.Name;
            }
            return new(paths, modes, names);
        }
        throw new InvalidOperationException("Display configuration is changing. Refresh and try again.");
    }
    public void Set(LayoutSnapshot snapshot, bool testOnly) => Check(LayoutNative.SetDisplayConfig(
        (uint)snapshot.Paths.Length, snapshot.Paths, (uint)snapshot.Modes.Length, snapshot.Modes, 0x20u | (testOnly ? 0x40u : 0x80u)));
}

internal sealed class DisplayLayout(ILayoutBackend backend)
{
    internal DisplayLayout() : this(new WindowsLayoutBackend()) { }
    internal LayoutSnapshot Read() => backend.Read();
    internal static bool SameState(LayoutSnapshot a, LayoutSnapshot b) => a.Connections == b.Connections &&
        a.Screens().Select(s => s with { Number = 0 }).OrderBy(s => s.Device).SequenceEqual(b.Screens().Select(s => s with { Number = 0 }).OrderBy(s => s.Device)) &&
        a.Paths.All(p => b.Paths.Any(q => p.SourceAdapter == q.SourceAdapter && p.SourceId == q.SourceId && p.TargetAdapter == q.TargetAdapter && p.TargetId == q.TargetId && p.Rotation == q.Rotation && p.RateNumerator == q.RateNumerator && p.RateDenominator == q.RateDenominator));

    internal ModePreview Apply(LayoutSnapshot original, IReadOnlyList<ScreenPlacement> requested)
    {
        if (!SameState(original, backend.Read())) throw new InvalidOperationException("Displays changed while arranging. Close the editor and refresh.");
        var error = LayoutGeometry.Validate(requested);
        if (error != null) throw new InvalidOperationException(error);
        var normalized = LayoutGeometry.Normalize(requested);
        if (LayoutGeometry.Validate(normalized) is { } normalizedError) throw new InvalidOperationException(normalizedError);
        var prior = original.Screens();
        if (prior.Count != normalized.Count || prior.Any(s => !normalized.Any(n => n.Device == s.Device && n.Width == s.Width && n.Height == s.Height && n.Primary == s.Primary)))
            throw new InvalidOperationException("Layout must preserve the connected displays, their sizes and the primary display.");
        var target = original.Copy();
        for (int i = 0; i < target.Paths.Length; i++)
        {
            var placement = normalized.Single(s => s.Device == target.Devices[i]);
            target.Modes[target.Paths[i].SourceMode].X = placement.X;
            target.Modes[target.Paths[i].SourceMode].Y = placement.Y;
        }
        backend.Set(target, true);
        var preview = new ModePreview(() =>
        {
            if (backend.Read().Connections != original.Connections)
                throw new InvalidOperationException("Connected displays changed. Restore the layout in Windows Display Settings.");
            backend.Set(original, false);
            if (!SameState(original, backend.Read())) throw new InvalidOperationException("Windows did not restore the previous layout.");
        });
        try
        {
            backend.Set(target, false);
            if (!SameState(target, backend.Read())) throw new InvalidOperationException("Windows did not apply the requested layout.");
            preview.Start(); return preview;
        }
        catch (Exception ex)
        {
            preview.Revert();
            throw new InvalidOperationException(ex.Message + " " + preview.Completion.Task.GetAwaiter().GetResult(), ex);
        }
    }
}
