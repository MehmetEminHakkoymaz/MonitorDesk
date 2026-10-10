namespace MonitorDesk.Services;

internal record MonitorProfile(string Id, int PhysicalIndex, int? Brightness, int? Contrast);
internal record LightingProfile(string Name, int Brightness, int Contrast, List<MonitorProfile>? Monitors = null)
{
    internal static IReadOnlyList<LightingProfile> Presets { get; } =
    [new("Night", 25, 60), new("Normal", 55, 70), new("High light", 90, 75)];
}

internal record ProfileControlResult(Display Display, bool Contrast, uint? Requested, string Status, string Detail);
internal record ProfileApplyResult(List<ProfileControlResult> Controls, List<Display>? ReadBack, string? ReadError)
{
    internal string Summary => $"{Controls.Count(c => c.Status == "Applied")} applied · {Controls.Count(c => c.Status == "Already set")} already set · {Controls.Count(c => c.Status == "Skipped")} skipped · {Controls.Count(c => c.Status == "Failed" || c.Status == "Different value" || c.Status == "Unverified")} need attention";
}

// Delegate injection keeps batch behavior testable without monitor writes.
internal sealed class LightingProfiles(Func<Task<List<Display>>> read, Func<Display, bool, uint, Task> write)
{
    internal static uint MapPercent(Level level, int percent)
    {
        if (percent is < 0 or > 100 || level.Max <= level.Min) throw new ArgumentOutOfRangeException(nameof(percent));
        return level.Min + (uint)Math.Round(((double)level.Max - level.Min) * percent / 100, MidpointRounding.AwayFromZero);
    }

    private static bool SameControl(Display a, Display b, bool contrast) => a.Id == b.Id &&
        (contrast || a.WmiInstance == null ? a.PhysicalIndex == b.PhysicalIndex : a.WmiInstance == b.WmiInstance);

    internal async Task<ProfileApplyResult> ApplyAsync(LightingProfile profile, IProgress<string>? progress = null, CancellationToken cancellation = default)
    {
        if (profile.Brightness is < 0 or > 100 || profile.Contrast is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(profile));
        if (profile.Monitors != null) CustomProfileStore.Validate(profile);
        cancellation.ThrowIfCancellationRequested();
        var displays = await read(); // Never write using a cached UI capability snapshot.
        var results = new List<ProfileControlResult>();
        var visited = new List<(Display Display, bool Contrast)>();
        foreach (var display in displays)
        {
            foreach (bool contrast in new[] { false, true })
            {
                cancellation.ThrowIfCancellationRequested();
                if (visited.Any(v => v.Contrast == contrast && SameControl(v.Display, display, contrast))) continue;
                string name = contrast ? "contrast" : "brightness";
                var saved = profile.Monitors?.FirstOrDefault(m => m.Id == display.Id && m.PhysicalIndex == display.PhysicalIndex);
                int? percent = profile.Monitors == null ? (contrast ? profile.Contrast : profile.Brightness)
                    : contrast ? saved?.Contrast : saved?.Brightness;
                if (percent == null)
                {
                    results.Add(new(display, contrast, null, "Skipped", "Not included in this profile."));
                    continue;
                }
                visited.Add((display, contrast));
                var level = contrast ? display.Contrast : display.Brightness;
                if (level == null || level.IsStale || level.Max <= level.Min)
                {
                    results.Add(new(display, contrast, null, "Skipped", level?.IsStale == true ? "Monitor did not respond; last-known value is not current." : "Control unavailable."));
                    continue;
                }
                uint target = MapPercent(level, percent.Value);
                if (target == level.Current)
                {
                    results.Add(new(display, contrast, target, "Already set", $"Current value is {target}.")); continue;
                }
                progress?.Report($"Applying {(profile.Monitors == null ? profile.Name.ToLowerInvariant() : profile.Name)} · Display {display.Number} {name}…");
                try
                {
                    await write(display, contrast, target);
                    results.Add(new(display, contrast, target, "Unverified", "Command sent; waiting for read-back."));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    results.Add(new(display, contrast, target, "Failed", ex.Message));
                }
            }
        }
        List<Display>? after = null; string? readError = null;
        try { after = await read(); }
        catch (Exception ex) { readError = ex.Message; }
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            if (result.Status is not ("Unverified" or "Already set")) continue;
            var current = after?.FirstOrDefault(d => SameControl(result.Display, d, result.Contrast));
            var value = result.Contrast ? current?.Contrast : current?.Brightness;
            if (value == null || value.IsStale)
                results[i] = result with { Status = "Unverified", Detail = "Current value could not be read. Refresh to verify." };
            else if (value.Current != result.Requested)
                results[i] = result with { Status = "Different value", Detail = $"Requested {result.Requested}; monitor reports {value.Current}. Device presets or supported brightness steps may limit the value." };
            else results[i] = result with { Status = result.Status == "Already set" ? "Already set" : "Applied", Detail = $"Verified value: {value.Current}." };
        }
        return new(results, after, readError);
    }
}
