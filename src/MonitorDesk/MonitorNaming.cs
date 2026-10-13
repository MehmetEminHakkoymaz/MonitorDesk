using System.Windows;
using System.Windows.Controls;
using MonitorDesk.Services;

namespace MonitorDesk;

public partial class MainWindow
{
    internal string DisplayName(Display display) => MonitorAliases.Name(display, preferences.MonitorNames);
    private void RenameMonitor(Display display)
    {
        var body = new StackPanel { Margin = new(22) };
        body.Children.Add(Text(L.Get("Monitor name"), 20));
        var original = Text(display.Name, 12, "Muted"); original.Margin = new(0, 8, 0, 12); body.Children.Add(original);
        var name = new TextBox { Text = preferences.MonitorNames?.FirstOrDefault(a => a.Id == display.Id && a.PhysicalIndex == display.PhysicalIndex)?.Name ?? "",
            MaxLength = 64, Padding = new(10), FontSize = 15 };
        name.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "Page");
        name.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "Ink");
        name.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "Line");
        System.Windows.Automation.AutomationProperties.SetName(name, L.Get("Monitor name")); body.Children.Add(name);
        var hint = Text(L.Get("Leave blank to use the original name. Saved for this monitor, including when disconnected."), 12, "Muted");
        hint.Margin = new(0, 10, 0, 18); body.Children.Add(hint);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = L.Get("Cancel"), IsCancel = true, Margin = new(0, 0, 8, 0) };
        var save = new Button { Content = L.Get("Save"), IsDefault = true, IsEnabled = preferencesStore != null };
        actions.Children.Add(cancel); actions.Children.Add(save); body.Children.Add(actions);
        var dialog = new Window { Owner = this, Title = L.Get("Monitor name"), Content = body, Width = 430,
            SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        dialog.SetResourceReference(BackgroundProperty, "Page");
        cancel.Click += (_, _) => dialog.Close();
        save.Click += (_, _) =>
        {
            string value = name.Text.Trim();
            if (value.Any(char.IsControl)) { hint.Text = L.Get("Enter a name on a single line, up to 64 characters."); return; }
            var aliases = preferences.MonitorNames?.Where(a => a.Id != display.Id || a.PhysicalIndex != display.PhysicalIndex).ToList() ?? [];
            if (value.Length > 0) aliases.Add(new(display.Id, display.PhysicalIndex, value));
            var next = preferences with { MonitorNames = aliases, LightTheme = light, EyeEnabled = warmEnabled, EyeStrength = (int)WarmStrength.Value };
            try
            {
                preferencesStore!.Save(next); preferences = next;
                Render(); QuickStateChanged?.Invoke(this, EventArgs.Empty); dialog.Close();
            }
            catch (Exception ex) { hint.Text = L.Get("Could not save preferences: ") + ex.Message; }
        };
        dialog.Loaded += (_, _) => { name.Focus(); name.SelectAll(); };
        dialog.ShowDialog();
    }
}
