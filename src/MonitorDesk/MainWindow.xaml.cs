using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using MonitorDesk.Services;

namespace MonitorDesk;
public partial class MainWindow : Window
{
    public TaskCompletionSource InitialRead { get; } = new();
    private readonly DisplayService service = new();
    private List<Display> displays = [];
    private bool busy, pending, light, closing;
    private readonly DispatcherTimer debounce = new() { Interval = TimeSpan.FromMilliseconds(750) };
    private readonly List<Window> labels = [];
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => { await RefreshAsync(); InitialRead.TrySetResult(); };
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
        debounce.Tick += async (_, _) => { debounce.Stop(); await RefreshAsync(); };
        Closed += (_, _) => { closing = true; SystemEvents.DisplaySettingsChanged -= DisplayChanged; debounce.Stop(); CloseLabels(); };
    }
    private void DisplayChanged(object? sender, EventArgs e)
    {
        if (closing) return;
        Dispatcher.BeginInvoke(() => { if (closing) return; debounce.Stop(); debounce.Start(); });
    }
    private void SetBusy(bool value)
    {
        busy = value; Toolbar.IsEnabled = !value; Cards.IsEnabled = !value;
        if (!value && pending && !closing) { pending = false; debounce.Start(); }
    }
    public async Task RefreshAsync()
    {
        if (busy) { pending = true; return; }
        SetBusy(true); Status.Text = "Reading displays and supported hardware controls…";
        try
        {
            displays = await service.ReadAsync();
            if (closing) return;
            Render(); Status.Text = displays.Any(d => d.Brightness?.IsStale == true || d.Contrast?.IsStale == true) ? "Some controls did not respond. Last-known values are marked and disabled; refresh to retry." : "Up to date. Move a slider, then choose Apply to change that setting.";
        }
        catch (Exception ex) { Summary.Text = displays.Count == 0 ? "Display discovery failed" : "Showing previous display information"; Status.Text = "Could not refresh displays: " + ex.Message; }
        finally { SetBusy(false); }
    }
    private static TextBlock Text(string value, double size = 14, string color = "Ink")
    {
        var text = new TextBlock { Text = value, FontSize = size, TextWrapping = TextWrapping.Wrap };
        text.SetResourceReference(TextBlock.ForegroundProperty, color); return text;
    }
    private void Render()
    {
        Cards.Children.Clear();
        Summary.Text = $"{displays.Count} connected display{(displays.Count == 1 ? "" : "s")}";
        if (displays.Count == 0) Cards.Children.Add(Text("No active displays were found. Connect a screen and refresh.", 16, "Muted"));
        foreach (var display in displays)
        {
            var content = new StackPanel();
            content.Children.Add(Text($"DISPLAY {display.Number:00}  {(display.Primary ? "· PRIMARY" : "· EXTENDED")}", 11, "Accent"));
            var title = Text(display.Name, 22); title.Margin = new(0, 8, 0, 8); title.FontWeight = FontWeights.SemiBold; content.Children.Add(title);
            content.Children.Add(Text($"{display.Width} × {display.Height}   /   {(display.Hertz > 1 ? $"{display.Hertz} Hz" : "Refresh rate unavailable")}", 14, "Muted"));
            var controls = new Grid { Margin = new(0, 20, 0, 16) };
            controls.ColumnDefinitions.Add(new()); controls.ColumnDefinitions.Add(new());
            var brightness = Control(display, false); brightness.Margin = new(0, 0, 24, 0);
            var contrast = Control(display, true); Grid.SetColumn(contrast, 1);
            controls.Children.Add(brightness); controls.Children.Add(contrast); content.Children.Add(controls);
            content.Children.Add(Text(display.Note, 12, "Muted"));
            var card = new Border { Child = content, Padding = new(24), CornerRadius = new(14), BorderThickness = new(1), Margin = new(0, 0, 0, 16) };
            card.SetResourceReference(Border.BackgroundProperty, "Card"); card.SetResourceReference(Border.BorderBrushProperty, "Line"); Cards.Children.Add(card);
        }
    }
    private StackPanel Control(Display display, bool contrast)
    {
        string name = contrast ? "Contrast" : "Brightness";
        Level? level = contrast ? display.Contrast : display.Brightness;
        var panel = new StackPanel();
        var caption = Text(name, 15); caption.FontWeight = FontWeights.SemiBold; panel.Children.Add(caption);
        if (level == null) { var unavailable = Text("Value unavailable. Refresh to retry.", 12, "Muted"); unavailable.Margin = new(0, 12, 0, 0); panel.Children.Add(unavailable); return panel; }
        var slider = new Slider { Minimum = level.Min, Maximum = level.Max, Value = level.Current, SmallChange = 1, LargeChange = 10, IsEnabled = !level.IsStale };
        System.Windows.Automation.AutomationProperties.SetName(slider, $"Display {display.Number} {name}");
        var value = Text(level.IsStale ? $"Last known: {level.Current} / {level.Max} · Not current" : $"{level.Current} / {level.Max}", 12, "Muted");
        var apply = new Button { Content = "Apply " + name.ToLowerInvariant(), HorizontalAlignment = HorizontalAlignment.Left, Margin = new(0, 12, 0, 0), IsEnabled = false };
        slider.ValueChanged += (_, _) => { value.Text = $"{Math.Round(slider.Value)} / {level.Max}"; apply.IsEnabled = !level.IsStale && (uint)Math.Round(slider.Value) != level.Current; };
        apply.Click += async (_, _) =>
        {
            SetBusy(true); Status.Text = $"Applying {name.ToLowerInvariant()} to display {display.Number}…";
            try
            {
                await service.SetAsync(display, contrast, (uint)Math.Round(slider.Value));
                displays = await service.ReadAsync();
                if (!closing) { Render(); Status.Text = $"{name} command sent. Display values have been read back."; }
            }
            catch (Exception ex) { Status.Text = $"Could not update {name.ToLowerInvariant()}: {ex.Message} Refresh before trying again."; }
            finally { SetBusy(false); }
        };
        panel.Children.Add(slider); panel.Children.Add(value); panel.Children.Add(apply);
        if (level.IsStale) panel.Children.Add(Text("Monitor did not respond. Refresh to reconnect.", 12, "Muted"));
        return panel;
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        light = !light;
        string[] keys = ["Page", "Card", "Ink", "Muted", "Line", "Accent"];
        string[] colors = light ? ["#F3F6FA", "#FFFFFF", "#172338", "#53627A", "#D9E1ED", "#087765"] : ["#0D111B", "#171E2D", "#F1F5FC", "#A2AEC5", "#2B364C", "#8BE5CE"];
        for (int i = 0; i < keys.Length; i++) Application.Current.Resources[keys[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
    }
    private void CloseLabels() { foreach (var label in labels) label.Close(); labels.Clear(); }
    private async void Identify_Click(object sender, RoutedEventArgs e)
    {
        CloseLabels();
        var current = new List<Window>();
        foreach (var display in displays.DistinctBy(d => d.Device))
        {
            var label = new Window { Width = 150, Height = 120, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, ShowActivated = false, Topmost = true, Background = new SolidColorBrush(Color.FromRgb(23, 30, 45)), Content = new TextBlock { Text = display.Number.ToString(), FontSize = 64, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            label.SourceInitialized += (_, _) => Native.SetWindowPos(new WindowInteropHelper(label).Handle, -1, display.Left + 40, display.Top + 40, 0, 0, 0x0011);
            labels.Add(label); current.Add(label); label.Show();
        }
        await Task.Delay(2500);
        foreach (var label in current) if (labels.Remove(label)) label.Close();
    }
}
