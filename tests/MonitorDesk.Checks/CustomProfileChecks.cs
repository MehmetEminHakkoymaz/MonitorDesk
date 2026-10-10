using System.IO;
using System.Text.Json;
using MonitorDesk.Services;

internal static class CustomProfileChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        var custom = new LightingProfile("Çalışma", 0, 0, [new("a", 0, 20, null), new("b", 0, 80, 65), new("disconnected", 0, 45, 60)]);
        string dir = Path.Combine(Path.GetTempPath(), "MonitorDesk-profiles-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "profiles.json");
        var store = new CustomProfileStore(path);
        try
        {
            check(store.Load().Count == 0 && !File.Exists(path), "Missing custom profile file starts empty without creating data");
            store.Save([custom]);
            var restored = store.Load().Single();
            check(restored.Name == custom.Name && restored.Monitors!.SequenceEqual(custom.Monitors!), "Custom monitor targets, Unicode names and skipped controls survive saving");
            store.Save([custom with { Name = "Düzenlenmiş" }]);
            check(store.Load().Single().Name == "Düzenlenmiş" && Directory.GetFiles(dir).Length == 1, "Editing atomically replaces profiles without temporary files");
            string original = File.ReadAllText(path);
            foreach (var bad in new[] { custom with { Name = " " }, custom with { Monitors = [new("a", 0, 101, null)] },
                custom with { Monitors = [new("a", 0, null, null)] }, custom with { Monitors = [new("a", 0, 10, null), new("a", 0, 20, null)] } })
            {
                try { store.Save([bad]); throw new Exception("Invalid profile accepted"); }
                catch (ArgumentException) { check(File.ReadAllText(path) == original, "Invalid profile cannot overwrite saved data"); }
            }
            try { store.Save([custom, custom with { Name = "çalışma" }]); throw new Exception("Duplicate names accepted"); }
            catch (ArgumentException) { check(true, "Custom profile names are unique ignoring case"); }
            store.Save([]); check(store.Load().Count == 0, "Deleting the last profile persists an empty list");
            File.WriteAllText(path, "broken JSON");
            try { store.Load(); throw new Exception("Corrupt profile file accepted"); }
            catch (JsonException) { check(File.ReadAllText(path) == "broken JSON", "Corrupt profile data is reported and left intact"); }
        }
        finally { foreach (var file in Directory.GetFiles(dir)) File.Delete(file); Directory.Delete(dir); }

        var a = new Display("a", "a", "a", 1, true, 0, 0, 1920, 1080, 60, 0, new(10, 50, 90), new(0, 50, 100), null, "test");
        List<Display> state = [a, a with { Id = "b", Device = "b", Number = 2, Brightness = new(0, 50, 100) }, a with { Id = "new", Device = "new", Number = 3 }];
        var writes = new List<(string Id, bool Contrast, uint Value)>(); int reads = 0;
        var applier = new LightingProfiles(() => { reads++; return Task.FromResult(state.ToList()); }, (d, contrast, value) =>
        {
            writes.Add((d.Id, contrast, value)); int i = state.FindIndex(x => x.Id == d.Id);
            state[i] = contrast ? state[i] with { Contrast = state[i].Contrast! with { Current = value } }
                : state[i] with { Brightness = state[i].Brightness! with { Current = value } }; return Task.CompletedTask;
        });
        var result = await applier.ApplyAsync(custom);
        check(writes.SequenceEqual(new[] { ("a", false, 26u), ("b", false, 80u), ("b", true, 65u) }), "Custom profiles apply different monitor percentages using actual hardware ranges");
        check(reads == 2 && result.Controls.Count(c => c.Status == "Applied") == 3, "Custom profiles use fresh capabilities and verify applied values");
        check(result.Controls.Count(c => c.Status == "Skipped") == 3 && !writes.Any(w => w.Id == "new" || w.Id == "disconnected"), "Unchecked controls and unknown or disconnected monitors receive no writes");
        writes.Clear(); state = [a with { Brightness = a.Brightness! with { IsStale = true } }];
        await applier.ApplyAsync(custom);
        check(writes.Count == 0, "Custom profiles never write stale controls");
        int before = reads;
        try { await applier.ApplyAsync(custom with { Monitors = [new("a", 0, -1, 50)] }); throw new Exception("Invalid target accepted"); }
        catch (ArgumentException) { check(reads == before, "Invalid custom target values reject before monitor access"); }
    }
}
