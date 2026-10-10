using System.Windows;
using System.Windows.Controls;
using MonitorDesk.Services;

namespace MonitorDesk;

public partial class MainWindow
{
    private List<LightingProfile> customProfiles = [];
    private CustomProfileStore? customProfileStore;
    private readonly StackPanel customProfileButtons = new() { Margin = new(0, 8, 0, 0) };
    internal IReadOnlyList<LightingProfile> CustomProfiles => customProfiles;
    internal void SetDiagnosticProfiles(List<LightingProfile> profiles)
    {
        if (customProfileStore != null) throw new InvalidOperationException("Only available in isolated diagnostics.");
        customProfiles = profiles; RenderCustomProfiles();
    }
    private void InitializeCustomProfiles(bool remember)
    {
        if (remember)
        {
            customProfileStore = new();
            try { customProfiles = customProfileStore.Load(); }
            catch (Exception ex) { preferenceLoadError = L.Get("Could not load custom profiles: ") + ex.Message; }
        }
        ProfilesPanel.Children.Insert(2, customProfileButtons);
        RenderCustomProfiles();
    }
    private void RenderCustomProfiles()
    {
        customProfileButtons.Children.Clear();
        foreach (var profile in customProfiles)
        {
            var row = new Grid { Margin = new(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            var apply = new Button { Content = new TextBlock { Text = profile.Name, TextTrimming = TextTrimming.CharacterEllipsis },
                HorizontalContentAlignment = HorizontalAlignment.Left, ToolTip = profile.Name };
            apply.Click += async (_, _) => await ApplyProfileAsync(profile);
            var edit = new Button { Content = "✎", Margin = new(6, 0, 0, 0), ToolTip = L.Get("Edit profile") };
            System.Windows.Automation.AutomationProperties.SetName(edit, L.Get("Edit profile") + ": " + profile.Name);
            edit.Click += (_, _) => CreateProfileEditor(profile).ShowDialog();
            Grid.SetColumn(edit, 1); row.Children.Add(apply); row.Children.Add(edit); customProfileButtons.Children.Add(row);
        }
        var add = new Button { Content = L.Get("+ New profile"), IsEnabled = displays.Count > 0 };
        add.Click += (_, _) => CreateProfileEditor().ShowDialog(); customProfileButtons.Children.Add(add);
        customProfileButtons.IsEnabled = !IsBusy;
    }
    internal Window CreateProfileEditor(LightingProfile? editing = null)
    {
        var body = new StackPanel { Margin = new(20) };
        body.Children.Add(Text(L.Get(editing == null ? "New profile" : "Edit profile"), 20));
        body.Children.Add(Text(L.Get("Profile name"), 12));
        var name = new TextBox { Text = editing?.Name ?? "", MaxLength = 64, Margin = new(0, 6, 0, 12), Padding = new(8) };
        name.SetResourceReference(TextBox.BackgroundProperty, "Page"); name.SetResourceReference(TextBox.ForegroundProperty, "Ink");
        body.Children.Add(name);
        body.Children.Add(Text(L.Get("Percentages of each monitor's range. Unchecked controls stay unchanged. Saving does not apply the profile."), 12, "Muted"));
        var editors = new List<Func<MonitorProfile>>();
        foreach (var display in displays)
        {
            var saved = editing?.Monitors?.FirstOrDefault(m => m.Id == display.Id && m.PhysicalIndex == display.PhysicalIndex);
            var card = new StackPanel { Margin = new(0, 14, 0, 0) };
            card.Children.Add(Text(L.Format("Display {0} · {1}", display.Number, display.Name), 13));
            Func<int?> MakeControl(bool contrast)
            {
                var level = contrast ? display.Contrast : display.Brightness;
                int? stored = contrast ? saved?.Contrast : saved?.Brightness;
                var enabled = new CheckBox { Content = L.Get(contrast ? "Contrast" : "Brightness"),
                    IsChecked = saved != null ? stored != null : level != null, Margin = new(0, 8, 0, 0) };
                enabled.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "Ink");
                int initial = stored ?? (level != null && level.Max > level.Min
                    ? (int)Math.Round(100.0 * (level.Current - level.Min) / (level.Max - level.Min)) : 50);
                var value = Text(initial + "%", 12, "Muted");
                var slider = new Slider { Minimum = 0, Maximum = 100, Value = initial, TickFrequency = 1,
                    IsSnapToTickEnabled = true, IsEnabled = enabled.IsChecked == true, Margin = new(0, 4, 0, 0) };
                System.Windows.Automation.AutomationProperties.SetName(slider, L.Format("Display {0} · {1}", display.Number, L.Get(contrast ? "Contrast" : "Brightness")));
                slider.ValueChanged += (_, _) => value.Text = ((int)slider.Value) + "%";
                enabled.Click += (_, _) => slider.IsEnabled = enabled.IsChecked == true;
                card.Children.Add(enabled); card.Children.Add(slider); card.Children.Add(value);
                if (level == null || level.IsStale) card.Children.Add(Text(L.Get("Unavailable controls are skipped when applying."), 11, "Muted"));
                return () => enabled.IsChecked == true ? (int)slider.Value : null;
            }
            var brightness = MakeControl(false); var contrast = MakeControl(true);
            editors.Add(() => new(display.Id, display.PhysicalIndex, brightness(), contrast()));
            body.Children.Add(card);
        }
        var disconnected = editing?.Monitors?.Where(m => !displays.Any(d => d.Id == m.Id && d.PhysicalIndex == m.PhysicalIndex)).ToList() ?? [];
        if (disconnected.Count > 0) body.Children.Add(Text(L.Get("Saved values for disconnected monitors are kept."), 12, "Muted"));
        var error = Text("", 12, "Muted");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new(0, 14, 0, 0) };
        var dialog = new Window { Title = L.Get(editing == null ? "New profile" : "Edit profile"), Owner = this, Width = 480,
            Height = 680, MinHeight = 400, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
        dialog.SetResourceReference(BackgroundProperty, "Page");
        bool Persist(List<LightingProfile> next)
        {
            try
            {
                if (customProfileStore == null) throw new InvalidOperationException(L.Get("Profile saving is disabled in diagnostics."));
                customProfileStore.Save(next); customProfiles = next; RenderCustomProfiles(); QuickStateChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex) { error.Text = L.Get("Could not save custom profiles: ") + ex.Message; return false; }
        }
        if (editing != null)
        {
            var delete = new Button { Content = L.Get("Delete"), Margin = new(0, 0, 8, 0) };
            delete.Click += (_, _) => { if (Persist(customProfiles.Where(p => p != editing).ToList())) dialog.Close(); };
            actions.Children.Add(delete);
        }
        var cancel = new Button { Content = L.Get("Cancel"), IsCancel = true, Margin = new(0, 0, 8, 0) };
        cancel.Click += (_, _) => dialog.Close(); actions.Children.Add(cancel);
        var save = new Button { Content = L.Get("Save") };
        save.Click += (_, _) =>
        {
            string title = name.Text.Trim();
            if (title.Length == 0 || customProfiles.Any(p => p != editing && p.Name.Equals(title, StringComparison.OrdinalIgnoreCase)))
            { error.Text = L.Get("Enter a unique profile name."); return; }
            var targets = editors.Select(e => e()).Concat(disconnected).ToList();
            if (!targets.Any(t => t.Brightness != null || t.Contrast != null))
            { error.Text = L.Get("Select at least one control."); return; }
            var updated = new LightingProfile(title, 0, 0, targets);
            var next = customProfiles.ToList();
            if (editing == null) next.Add(updated); else next[next.IndexOf(editing)] = updated;
            if (Persist(next)) dialog.Close();
        };
        actions.Children.Add(save);
        var footer = new StackPanel { Margin = new(20, 8, 20, 16) };
        footer.Children.Add(error); footer.Children.Add(actions);
        var frame = new Grid(); frame.RowDefinitions.Add(new()); frame.RowDefinitions.Add(new() { Height = GridLength.Auto });
        frame.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Grid.SetRow(footer, 1); frame.Children.Add(footer); dialog.Content = frame;
        return dialog;
    }
}
