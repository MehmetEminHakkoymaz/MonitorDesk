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
    private readonly UniformGrid profiles = new() { Columns = 3, Margin = new(0, 12, 0, 12) };
    private readonly Button eye = new() { Height = 46 };
    private readonly TextBlock eyeState = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly System.Windows.Shapes.Path eyeShape = new() { Data = Geometry.Parse("M 1,12 Q 15,-3 29,12 Q 15,27 1,12 Z M 19,12 A 4,4 0 1 1 11,12 A 4,4 0 1 1 19,12"), Width = 30, Height = 24, StrokeThickness = 1.8, Margin = new(0, 0, 10, 0) };

    internal TrayPanel(MainWindow main)
    {
        this.main = main;
        Title = "MonitorDesk quick controls"; Width = 390; MaxHeight = 800;
        SizeToContent = SizeToContent.Height; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false; Topmost = true; FontFamily = new("Segoe UI"); FontSize = 12;
        SetResourceReference(BackgroundProperty, "Page");
        var body = new StackPanel();
        var header = new Grid { Margin = new(0, 0, 0, 14) };
        header.Children.Add(new TextBlock { Text = "MonitorDesk", FontSize = 18, FontWeight = FontWeights.SemiBold });
        var refresh = new Button { Content = "↻", ToolTip = "Refresh displays", HorizontalAlignment = HorizontalAlignment.Right };
        refresh.Click += async (_, _) => await main.RefreshAsync();
        header.Children.Add(refresh); body.Children.Add(header); body.Children.Add(monitors);
        foreach (var profile in LightingProfile.Presets)
        {
            var icon = new LightingIcon(profile.Brightness / 100.0) { Width = 30, Height = 30, HorizontalAlignment = HorizontalAlignment.Center };
            icon.SetResourceReference(LightingIcon.InkProperty, "Accent");
            var label = new StackPanel(); label.Children.Add(icon);
            label.Children.Add(new TextBlock { Text = profile.Name, TextAlignment = TextAlignment.Center, Margin = new(0, 4, 0, 0) });
            var button = new Button { Content = label, Margin = new(0, 0, 6, 0), ToolTip = $"{profile.Name}: brightness {profile.Brightness}%, contrast {profile.Contrast}%" };
            button.Click += async (_, _) => await main.ApplyProfileAsync(profile); profiles.Children.Add(button);
        }
        body.Children.Add(profiles);
        var eyeContent = new StackPanel { Orientation = Orientation.Horizontal };
        eyeContent.Children.Add(eyeShape); eyeContent.Children.Add(eyeState); eye.Content = eyeContent;
        eye.ToolTip = "Toggle warm eye comfort filter";
        eye.Click += (_, _) => main.ToggleEyeComfort(); body.Children.Add(eye);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Margin = new(0, 12, 0, 0) };
        status.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
        status.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = main.Status }); body.Children.Add(status);
        var frame = new Border { Padding = new(18), BorderThickness = new(1), Child = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
        frame.SetResourceReference(Border.BorderBrushProperty, "Line"); Content = frame;
        main.QuickStateChanged += QuickStateChanged;
        Deactivated += (_, _) => Hide();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { Hide(); e.Handled = true; } };
        Closed += (_, _) => main.QuickStateChanged -= QuickStateChanged;
        Render();
    }

    private void QuickStateChanged(object? sender, EventArgs e) { if (IsVisible) Render(); }
    internal void Render()
    {
        monitors.Children.Clear();
        foreach (var display in main.QuickDisplays)
        {
            var controls = new StackPanel { IsEnabled = !main.IsBusy };
            controls.Children.Add(new TextBlock { Text = $"Display {display.Number} · {display.Name}", FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = display.Name, Margin = new(0, 0, 0, 10) });
            controls.Children.Add(main.Control(display, false, true));
            var contrast = main.Control(display, true, true); contrast.Margin = new(0, 10, 0, 0); controls.Children.Add(contrast);
            var card = new Border { Child = controls, Padding = new(12), CornerRadius = new(10), Margin = new(0, 0, 0, 10) };
            card.SetResourceReference(Border.BackgroundProperty, "Card"); monitors.Children.Add(card);
        }
        if (main.QuickDisplays.Count == 0) monitors.Children.Add(new TextBlock { Text = "No displays available. Refresh to retry.", TextWrapping = TextWrapping.Wrap });
        profiles.IsEnabled = !main.IsBusy;
        eyeState.Text = main.EyeComfortEnabled ? "Eye comfort · On" : "Eye comfort · Off";
        eyeShape.SetResourceReference(System.Windows.Shapes.Path.StrokeProperty, main.EyeComfortEnabled ? "Accent" : "Muted");
        eye.SetResourceReference(Button.BorderBrushProperty, main.EyeComfortEnabled ? "Accent" : "Line");
    }
}
