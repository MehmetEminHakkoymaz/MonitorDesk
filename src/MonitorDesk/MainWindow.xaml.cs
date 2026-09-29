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
    private bool changingMode;
    private ModePreview? modePreview;
    private readonly DispatcherTimer debounce = new() { Interval = TimeSpan.FromMilliseconds(750) };
    private readonly List<Window> labels = [];
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => { await RefreshAsync(); InitialRead.TrySetResult(); };
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
        debounce.Tick += async (_, _) => { debounce.Stop(); await RefreshAsync(); };
        Closing += (_, e) => { if (changingMode) { e.Cancel = true; modePreview?.Revert(); } };
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
            content.Children.Add(ModeControl(display));
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
    private StackPanel ModeControl(Display display)
    {
        var panel = new StackPanel { Margin = new(0, 18, 0, 0) };
        panel.Children.Add(Text("Resolution and refresh rate", 15));
        List<DisplayMode> modes;
        try { modes = DisplayModes.Read(display.Device); }
        catch (Exception ex) { panel.Children.Add(Text(ex.Message, 12, "Muted")); return panel; }
        if (modes.Count == 0) { panel.Children.Add(Text("No compatible display modes available.", 12, "Muted")); return panel; }
        var row = new WrapPanel { Margin = new(0, 8, 0, 8) };
        var resolution = new ComboBox { MinWidth = 160, Margin = new(0, 0, 12, 8), Foreground = Brushes.Black, Background = Brushes.White };
        var rate = new ComboBox { MinWidth = 110, Margin = new(0, 0, 12, 8), Foreground = Brushes.Black, Background = Brushes.White };
        // The application's implicit TextBlock style otherwise overrides native combo text colors.
        foreach (var combo in new[] { resolution, rate })
        {
            var textStyle = new Style(typeof(TextBlock));
            textStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            combo.Resources.Add(typeof(TextBlock), textStyle);
        }
        System.Windows.Automation.AutomationProperties.SetName(resolution, $"Display {display.Number} resolution");
        System.Windows.Automation.AutomationProperties.SetName(rate, $"Display {display.Number} refresh rate");
        var apply = new Button { Content = "Preview mode", IsEnabled = false, Margin = new(0, 0, 0, 8) };
        var sizes = modes.Select(m => (m.Width, m.Height)).Distinct().ToList();
        resolution.ItemsSource = sizes.Select(s => new ComboBoxItem
        {
            Content = new TextBlock { Text = $"{s.Width} × {s.Height}", Foreground = Brushes.Black }
        }).ToList();
        DisplayMode? Selected() => resolution.SelectedIndex >= 0 && rate.SelectedItem is ComboBoxItem { Tag: uint hz }
            ? new(sizes[resolution.SelectedIndex].Width, sizes[resolution.SelectedIndex].Height, hz, display.Orientation) : null;
        void UpdateApply() => apply.IsEnabled = Selected() is { } m && m != new DisplayMode((uint)display.Width, (uint)display.Height, display.Hertz, display.Orientation);
        resolution.SelectionChanged += (_, _) =>
        {
            if (resolution.SelectedIndex < 0) return;
            var size = sizes[resolution.SelectedIndex];
            rate.Items.Clear();
            foreach (var m in modes.Where(m => m.Width == size.Width && m.Height == size.Height))
                rate.Items.Add(new ComboBoxItem { Content = new TextBlock { Text = $"{m.Hertz} Hz", Foreground = Brushes.Black }, Tag = m.Hertz, Foreground = Brushes.Black });
            rate.SelectedIndex = -1;
            for (int i = 0; i < rate.Items.Count; i++)
                if ((uint)((ComboBoxItem)rate.Items[i]).Tag == display.Hertz) rate.SelectedIndex = i;
            if (rate.SelectedIndex < 0) rate.SelectedIndex = rate.Items.Count - 1;
            UpdateApply();
        };
        rate.SelectionChanged += (_, _) => UpdateApply();
        resolution.SelectedIndex = sizes.FindIndex(s => s.Width == display.Width && s.Height == display.Height);
        apply.Click += async (_, _) => { if (Selected() is { } mode) await PreviewModeAsync(display, mode); };
        row.Children.Add(resolution); row.Children.Add(rate); row.Children.Add(apply); panel.Children.Add(row);
        panel.Children.Add(Text("Orientation", 15));
        var rotationRow = new WrapPanel { Margin = new(0, 8, 0, 8) };
        var orientation = new ComboBox { MinWidth = 210, Margin = new(0, 0, 12, 8), Foreground = Brushes.Black, Background = Brushes.White };
        System.Windows.Automation.AutomationProperties.SetName(orientation, $"Display {display.Number} orientation");
        var active = new Native.DevMode { Width = (uint)display.Width, Height = (uint)display.Height, Frequency = display.Hertz, Orientation = display.Orientation };
        for (uint value = 0; value < 4; value++)
        {
            var rotated = DisplayModes.Describe(DisplayModes.Rotate(active, value));
            orientation.Items.Add(new ComboBoxItem { Tag = rotated, Content = new TextBlock { Text = rotated.OrientationName, Foreground = Brushes.Black } });
        }
        orientation.SelectedIndex = (int)display.Orientation;
        var rotate = new Button { Content = "Preview orientation", IsEnabled = false, Margin = new(0, 0, 0, 8) };
        orientation.SelectionChanged += (_, _) => rotate.IsEnabled = orientation.SelectedItem is ComboBoxItem { Tag: DisplayMode m } && m.Orientation != display.Orientation;
        rotate.Click += async (_, _) => { if (orientation.SelectedItem is ComboBoxItem { Tag: DisplayMode mode }) await PreviewModeAsync(display, mode); };
        rotationRow.Children.Add(orientation); rotationRow.Children.Add(rotate); panel.Children.Add(rotationRow);
        panel.Children.Add(Text("Rotation uses the current resolution and refresh rate, swapping width and height when needed. Windows checks support before applying.", 12, "Muted"));
        panel.Children.Add(Text("Confirm within 15 seconds or the previous mode will be restored. Kept changes last for this Windows session. Mirrored screens may change together.", 12, "Muted"));
        return panel;
    }

    private Task PreviewModeAsync(Display display, DisplayMode mode) =>
        PreviewChangeAsync(() => DisplayModes.Apply(display, mode), mode.ToString());

    private async void Arrange_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            var layout = new DisplayLayout();
            var snapshot = await Task.Run(layout.Read);
            if (closing) return;
            var screens = snapshot.Screens().Select(s => s with { Number = displays.FirstOrDefault(d => d.Device == s.Device)?.Number ?? s.Number }).ToList();
            if (screens.Count < 2) throw new InvalidOperationException("Connect at least two extended displays to arrange them.");
            var editor = new LayoutEditor(screens) { Owner = this };
            if (editor.ShowDialog() == true)
            {
                var requested = editor.Placements;
                await PreviewChangeAsync(() => layout.Apply(snapshot, requested), "this screen layout");
            }
        }
        catch (Exception ex) { Status.Text = "Could not arrange screens: " + ex.Message; }
        finally { SetBusy(false); }
    }

    private async Task PreviewChangeAsync(Func<ModePreview> apply, string description)
    {
        changingMode = true; SetBusy(true); CloseLabels();
        Status.Text = $"Testing {description}…";
        string result;
        try
        {
            modePreview = await Task.Run(apply);
            var preview = modePreview;
            var body = new StackPanel { Margin = new(24) };
            body.Children.Add(Text($"Keep {description}?", 22));
            var countdown = Text("", 14, "Muted"); countdown.Margin = new(0, 16, 0, 16); body.Children.Add(countdown);
            var buttons = new WrapPanel();
            var keep = new Button { Content = "Keep for this session", Margin = new(0, 0, 12, 0) };
            var revert = new Button { Content = "Revert", IsCancel = true };
            buttons.Children.Add(keep); buttons.Children.Add(revert); body.Children.Add(buttons);
            var dialog = new Window { Title = "Confirm display mode", Owner = this, Content = body, Width = 520,
                SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen, Topmost = true };
            dialog.SetResourceReference(BackgroundProperty, "Page");
            var ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            void UpdateCountdown() => countdown.Text = $"Reverting in {Math.Max(0, (int)Math.Ceiling((preview.Deadline - DateTime.UtcNow).TotalSeconds))} seconds unless you keep this mode.";
            ticker.Tick += (_, _) => UpdateCountdown();
            keep.Click += (_, _) => preview.Keep();
            revert.Click += (_, _) => preview.Revert();
            dialog.Closed += (_, _) => preview.Revert();
            dialog.PreviewKeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) { preview.Revert(); e.Handled = true; } };
            UpdateCountdown(); ticker.Start();
            try { dialog.Show(); revert.Focus(); result = await preview.Completion.Task; }
            finally { ticker.Stop(); dialog.Close(); }
        }
        catch (Exception ex) { result = "Could not preview display mode: " + ex.Message; }
        finally { modePreview?.Dispose(); modePreview = null; changingMode = false; SetBusy(false); }
        await RefreshAsync();
        debounce.Stop(); pending = false;
        Status.Text = result;
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
