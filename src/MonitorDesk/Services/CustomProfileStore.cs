using System.IO;
using System.Text.Json;

namespace MonitorDesk.Services;

internal sealed class CustomProfileStore(string? path = null)
{
    private readonly string path = path ?? System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonitorDesk", "profiles.json");
    internal static void Validate(LightingProfile profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 64 || profile.Monitors == null || profile.Monitors.Count == 0 ||
            profile.Monitors.Any(m => m == null || string.IsNullOrWhiteSpace(m.Id) || m.PhysicalIndex < 0 ||
                m.Brightness is < 0 or > 100 || m.Contrast is < 0 or > 100) ||
            !profile.Monitors.Any(m => m.Brightness != null || m.Contrast != null) ||
            profile.Monitors.Select(m => (m.Id, m.PhysicalIndex)).Distinct().Count() != profile.Monitors.Count)
            throw new ArgumentException("Invalid custom profile.");
    }
    private static void ValidateAll(List<LightingProfile> profiles)
    {
        foreach (var profile in profiles) Validate(profile);
        if (profiles.Select(p => p.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != profiles.Count)
            throw new ArgumentException("Profile names must be unique.");
    }
    internal List<LightingProfile> Load()
    {
        var profiles = File.Exists(path) ? JsonSerializer.Deserialize<List<LightingProfile>>(File.ReadAllText(path))
            ?? throw new JsonException("Expected a profile list.") : [];
        ValidateAll(profiles); return profiles;
    }
    internal void Save(List<LightingProfile> profiles)
    {
        ValidateAll(profiles);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
