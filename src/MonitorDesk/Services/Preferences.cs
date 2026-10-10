using System.IO;
using System.Text.Json;
using System.Windows;

namespace MonitorDesk.Services;

internal record WindowPlacement(int Left, int Top, double Width, double Height);
internal record Preferences(bool LightTheme = false, int EyeStrength = 40,
    WindowPlacement? Window = null, bool Maximized = false, bool EyeEnabled = false)
{
    internal Preferences Validated() => this with
    {
        EyeStrength = Math.Clamp(EyeStrength, 0, 90),
        Window = Window is { } w && double.IsFinite(w.Width) && double.IsFinite(w.Height)
            ? w with { Left = Math.Clamp(w.Left, -200000, 200000), Top = Math.Clamp(w.Top, -200000, 200000),
                Width = Math.Clamp(w.Width, 680, 6000), Height = Math.Clamp(w.Height, 520, 4000) } : null
    };
}

internal sealed class PreferencesStore(string? path = null)
{
    internal string Path { get; } = path ?? System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonitorDesk", "settings.json");
    internal Preferences Load() => File.Exists(Path)
        ? (JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Path)) ?? new Preferences()).Validated() : new();
    internal void Save(Preferences preferences)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        string temp = Path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(preferences.Validated(), new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, Path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

internal static class PlacementGeometry
{
    // Stored position is in native desktop pixels; fit it to a currently connected work area.
    internal static Rect Fit(Rect requested, IReadOnlyList<Rect> workAreas)
    {
        if (workAreas.Count == 0) throw new ArgumentException("No work areas", nameof(workAreas));
        static double Overlap(Rect a, Rect b) { a.Intersect(b); return a.IsEmpty ? 0 : a.Width * a.Height; }
        static double Distance(Rect a, Rect b)
        {
            double dx = Math.Max(b.Left - a.Right, Math.Max(a.Left - b.Right, 0));
            double dy = Math.Max(b.Top - a.Bottom, Math.Max(a.Top - b.Bottom, 0));
            return dx * dx + dy * dy;
        }
        var area = workAreas.OrderByDescending(a => Overlap(requested, a)).ThenBy(a => Distance(requested, a)).First();
        double width = Math.Min(requested.Width, area.Width), height = Math.Min(requested.Height, area.Height);
        return new(Math.Clamp(requested.Left, area.Left, area.Right - width),
            Math.Clamp(requested.Top, area.Top, area.Bottom - height), width, height);
    }
}
