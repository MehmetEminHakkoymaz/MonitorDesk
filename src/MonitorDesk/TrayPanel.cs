using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MonitorDesk.Services;

namespace MonitorDesk;

internal sealed class TrayPanel : Window
{
    private readonly MainWindow main;
    private readonly StackPanel monitors = new();
    private readonly UniformGrid customProfiles = new() { Columns = 3, Margin = new(0, 0, 0, 8) };
    private readonly UniformGrid profiles = new() { Columns = 3, Margin = new(0, 4, 0, 8) };
    private readonly Button eye = new() { Height = 32 };
    private readonly TextBlock eyeState = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly System.Windows.Shapes.Path eyeShape = new() { Data = Geometry.Parse("M 1,12 Q 15,-3 29,12 Q 15,27 1,12 Z M 19,12 A 4,4 0 1 1 11,12 A 4,4 0 1 1 19,12"), Width = 30, Height = 24, StrokeThickness = 1.8, Margin = new(0, 0, 10, 0) };

    internal TrayPanel(MainWindow main)
    {
        this.main = main;
        Title = L.Get("MonitorDesk quick controls"); Width = 320; MaxHeight = 560;
        SizeToContent = SizeToContent.Height; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false; Topmost = true; FontFamily = new("Segoe UI"); FontSize = 12;
        SetResourceReference(BackgroundProperty, "Page");
        var body = new StackPanel();
        var header = new Grid { Margin = new(0, 0, 0, 10) };
        header.Children.Add(new TextBlock { Text = "MonitorDesk", FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        var refresh = new Button { Content = "↻", Padding = new(7, 3, 7, 3), ToolTip = L.Get("Refresh displays"), HorizontalAlignment = HorizontalAlignment.Right };
        refresh.Click += async (_, _) => await main.RefreshAsync();
        header.Children.Add(refresh); body.Children.Add(header); body.Children.Add(monitors);
        foreach (var profile in LightingProfile.Presets)
        {
            var icon = new LightingIcon(profile.Brightness / 100.0) { Width = 36, Height = 36 };
            icon.SetResourceReference(LightingIcon.InkProperty, "Accent");
            var label = new StackPanel { Orientation = Orientation.Horizontal };
            label.Children.Add(new Viewbox { Width = 20, Height = 20, Child = icon, Margin = new(0, 0, 4, 0) });
            label.Children.Add(new TextBlock { Text = L.Get(profile.Name), FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
            var button = new Button { Content = label, Height = 34, Padding = new(4, 3, 4, 3), Margin = new(0, 0, 4, 0), ToolTip = L.Format("{0}: brightness {1}%, contrast {2}%", L.Get(profile.Name), profile.Brightness, profile.Contrast) };
            button.Click += async (_, _) => await main.ApplyProfileAsync(profile); profiles.Children.Add(button);
        }
        body.Children.Add(profiles);
        body.Children.Add(customProfiles);
        var eyeContent = new StackPanel { Orientation = Orientation.Horizontal };
        eyeShape.Margin = new(0);
        eyeContent.Children.Add(new Viewbox { Width = 22, Height = 18, Child = eyeShape, Margin = new(0, 0, 8, 0) }); eyeContent.Children.Add(eyeState); eye.Content = eyeContent;
        eye.ToolTip = L.Get("Toggle warm eye comfort filter");
        eye.Click += (_, _) => main.ToggleEyeComfort(); body.Children.Add(eye);
        var status = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 10, Margin = new(0, 8, 0, 0) };
        status.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
        status.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = main.Status }); body.Children.Add(status);
        status.SetBinding(ToolTipProperty, new Binding("Text") { Source = main.Status });
        var frame = new Border { Padding = new(12), BorderThickness = new(1), Child = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
        frame.SetResourceReference(Border.BorderBrushProperty, "Line"); Content = frame;
        main.QuickStateChanged += QuickStateChanged;
        Deactivated += (_, _) => Hide();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { Hide(); e.Handled = true; } };
        Closed += (_, _) => main.QuickStateChanged -= QuickStateChanged;
        Render();
    }

    private void QuickStateChanged(object? sender, EventArgs e)
    {
        if (!IsVisible) return;
        if (main.AutomaticLevelsActive) UpdateState();
        else Render();
    }
    internal void Render()
    {
        customProfiles.Children.Clear();
        foreach (var profile in main.CustomProfiles)
        {
            var button = new Button { Content = new TextBlock { Text = profile.Name, TextTrimming = TextTrimming.CharacterEllipsis },
                Height = 34, Padding = new(4, 3, 4, 3), Margin = new(0, 0, 4, 4), ToolTip = profile.Name };
            button.Click += async (_, _) => await main.ApplyProfileAsync(profile); customProfiles.Children.Add(button);
        }
        monitors.Children.Clear();
        foreach (var display in main.QuickDisplays)
        {
            var controls = new StackPanel { IsEnabled = !main.ControlsBusy };
            controls.Children.Add(new TextBlock { Text = L.Format("Display {0} · {1}", display.Number, display.Name), FontSize = 11, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = display.Name, Margin = new(0, 0, 0, 6) });
            controls.Children.Add(main.Control(display, false, true));
            var contrast = main.Control(display, true, true); contrast.Margin = new(0, 4, 0, 0); controls.Children.Add(contrast);
            var card = new Border { Child = controls, Padding = new(8), CornerRadius = new(8), Margin = new(0, 0, 0, 6) };
            card.SetResourceReference(Border.BackgroundProperty, "Card"); monitors.Children.Add(card);
        }
        if (main.QuickDisplays.Count == 0) monitors.Children.Add(new TextBlock { Text = L.Get("No displays available. Refresh to retry."), TextWrapping = TextWrapping.Wrap });
        UpdateState();
    }
    private void UpdateState()
    {
        profiles.IsEnabled = !main.IsBusy;
        customProfiles.IsEnabled = !main.IsBusy;
        eyeState.Text = L.Format("Eye comfort · {0} · {1} / 90", L.Get(main.EyeComfortEnabled ? "On" : "Off"), main.EyeComfortStrength);
        eye.ToolTip = L.Format("Warm filter {0}. Selected strength: {1} / 90. Click to toggle.", L.Get(main.EyeComfortEnabled ? "enabled" : "disabled"), main.EyeComfortStrength);
        eyeShape.SetResourceReference(System.Windows.Shapes.Path.StrokeProperty, main.EyeComfortEnabled ? "Accent" : "Muted");
        eye.SetResourceReference(Button.BorderBrushProperty, main.EyeComfortEnabled ? "Accent" : "Line");
    }
}
