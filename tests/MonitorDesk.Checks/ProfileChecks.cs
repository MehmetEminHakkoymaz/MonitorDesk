using MonitorDesk.Services;

internal static class ProfileChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        check(LightingProfiles.MapPercent(new(10, 30, 90), 25) == 30, "Profile percentages honor nonzero monitor minima");
        check(LightingProfiles.MapPercent(new(10, 30, 90), 0) == 10 && LightingProfiles.MapPercent(new(10, 30, 90), 100) == 90, "Profile endpoints map to hardware bounds");
        check(LightingProfiles.MapPercent(new(0, 0, uint.MaxValue), 100) == uint.MaxValue, "Profile mapping avoids unsigned range overflow");
        foreach (int percent in new[] { -1, 101 })
        {
            try { LightingProfiles.MapPercent(new(0, 50, 100), percent); throw new Exception("Invalid percentage accepted"); }
            catch (ArgumentOutOfRangeException) { check(true, "Out-of-range profile percentage is rejected"); }
        }
        var first = new Display("a", "a", "a", 1, true, 0, 0, 1920, 1080, 60, 0, new(10, 50, 90), null, null, "test");
        var second = first with { Id = "b", Device = "b", Number = 2, Brightness = new(0, 50, 100), Contrast = new(0, 50, 100) };
        var third = first with { Id = "c", Device = "c", Number = 3, Brightness = new(0, 50, 100, true), Contrast = new(0, 70, 100) };
        List<Display> state = [first, second, third];
        int reads = 0;
        var writes = new List<(string Id, bool Contrast, uint Value)>();
        var profiles = new LightingProfiles(() => { reads++; return Task.FromResult(state.ToList()); }, (d, contrast, value) =>
        {
            writes.Add((d.Id, contrast, value));
            int i = state.FindIndex(s => s.Id == d.Id);
            state[i] = contrast ? state[i] with { Contrast = state[i].Contrast! with { Current = value } } : state[i] with { Brightness = state[i].Brightness! with { Current = value } };
            return Task.CompletedTask;
        });
        var normal = await profiles.ApplyAsync(LightingProfile.Presets[1]);
        check(reads == 2, "Profiles read current capabilities before writing and read back afterward");
        check(writes.SequenceEqual(new[] { ("a", false, 54u), ("b", false, 55u), ("b", true, 70u) }), "Profile batch maps values and skips unavailable, stale and unchanged controls");
        check(normal.Controls.Count(c => c.Status == "Applied") == 3 && normal.Controls.Count(c => c.Status == "Skipped") == 2 && normal.Controls.Count(c => c.Status == "Already set") == 1, "Profile results report each control honestly");
        state = [second]; writes.Clear();
        profiles = new(() => Task.FromResult(state.ToList()), (d, contrast, value) =>
        {
            if (!contrast) throw new InvalidOperationException("Driver refused brightness");
            state[0] = state[0] with { Contrast = new(0, value, 100) }; return Task.CompletedTask;
        });
        var partial = await profiles.ApplyAsync(LightingProfile.Presets[0]);
        check(partial.Controls[0].Status == "Failed" && partial.Controls[1].Status == "Applied", "One write failure does not prevent remaining controls from applying");
        profiles = new(() => Task.FromResult(new List<Display> { second }), (_, _, _) => Task.CompletedTask);
        var mismatch = await profiles.ApplyAsync(LightingProfile.Presets[0]);
        check(mismatch.Controls.All(c => c.Status == "Different value"), "Successful commands with mismatching read-back are not reported as applied");
        reads = 0;
        profiles = new(() => ++reads == 1 ? Task.FromResult(new List<Display> { second }) : throw new InvalidOperationException("Read failed"), (_, _, _) => Task.CompletedTask);
        var unverified = await profiles.ApplyAsync(LightingProfile.Presets[0]);
        check(unverified.Controls.All(c => c.Status == "Unverified") && unverified.ReadBack == null && unverified.ReadError == "Read failed", "Failed read-back leaves commands unverified and preserves the error");
        profiles = new(() => Task.FromResult(new List<Display> { second, second }), (_, contrast, value) => { writes.Add(("b", contrast, value)); return Task.CompletedTask; });
        writes.Clear(); await profiles.ApplyAsync(LightingProfile.Presets[0]);
        check(writes.Count == 2, "Duplicate physical controls are written only once");
        reads = 0;
        profiles = new(() => { reads++; return Task.FromResult(new List<Display>()); }, (_, _, _) => throw new Exception("Unexpected write"));
        try { await profiles.ApplyAsync(new("Invalid", 101, 60)); throw new Exception("Invalid profile accepted"); }
        catch (ArgumentOutOfRangeException) { check(reads == 0, "Invalid profiles reject before native discovery or writes"); }
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { await profiles.ApplyAsync(LightingProfile.Presets[0], cancellation: cancelled.Token); throw new Exception("Cancelled profile accepted"); }
        catch (OperationCanceledException) { check(reads == 0, "Cancelled profiles stop before native access"); }
        var empty = await profiles.ApplyAsync(LightingProfile.Presets[0]);
        check(empty.Controls.Count == 0, "Empty monitor lists produce an empty result without writes");
    }
}
