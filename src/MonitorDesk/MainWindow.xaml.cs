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
    private readonly CancellationTokenSource profileCancellation = new();
    private readonly DispatcherTimer debounce = new() { Interval = TimeSpan.FromMilliseconds(750) };
    private readonly List<Window> labels = [];
    private readonly WarmFilter warmFilter = new();
    private bool warmEnabled;
    internal bool HideOnClose { get; set; }
    private bool exitRequested;
    private bool hideAfterPreview;
    internal event EventHandler? QuickStateChanged;
    internal IReadOnlyList<Display> QuickDisplays => displays;
    internal bool IsBusy => busy;
    internal bool EyeComfortEnabled => warmEnabled;
    internal void ExitApplication()
    {
        exitRequested = true;
        Close();
    }
    public MainWindow()
    {
        InitializeComponent();
        foreach (var profile in LightingProfile.Presets)
        {
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            var icon = new LightingIcon(profile.Brightness / 100.0) { Width = 36, Height = 36, Margin = new(0, 0, 12, 0) };
            icon.SetResourceReference(LightingIcon.InkProperty, "Accent");
            content.Children.Add(icon);
            content.Children.Add(new TextBlock { Text = profile.Name, VerticalAlignment = VerticalAlignment.Center, FontSize = 14 });
            var button = new Button { Content = content, Height = 66, Margin = new(0, 0, 0, 8), ToolTip = $"Apply {profile.Name.ToLowerInvariant()}: brightness {profile.Brightness}%, contrast {profile.Contrast}% on all supported monitors. Unavailable controls are skipped." };
            System.Windows.Automation.AutomationProperties.SetName(button, $"Apply {profile.Name} lighting profile to all monitors");
            button.Click += async (_, _) => await ApplyProfileAsync(profile);
            ProfileButtons.Children.Add(button);
        }
        Cards.SizeChanged += (_, _) => SizeCards();
        TopSections.SizeChanged += (_, _) => SizeSections();
        Loaded += async (_, _) => { await RefreshAsync(); InitialRead.TrySetResult(); };
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
        debounce.Tick += async (_, _) => { debounce.Stop(); await RefreshAsync(); };
        Closing += (_, e) =>
        {
            if (changingMode) { e.Cancel = true; hideAfterPreview = HideOnClose && !exitRequested; modePreview?.Revert(); return; }
            if (HideOnClose && !exitRequested) { e.Cancel = true; CloseLabels(); Hide(); }
        };
        Closed += (_, _) => { closing = true; warmFilter.Dispose(); profileCancellation.Cancel(); SystemEvents.DisplaySettingsChanged -= DisplayChanged; debounce.Stop(); CloseLabels(); };
    }
    private void DisplayChanged(object? sender, EventArgs e)
    {
        if (closing) return;
        Dispatcher.BeginInvoke(() => { if (closing) return; debounce.Stop(); debounce.Start(); });
    }
    private void SetBusy(bool value)
    {
        busy = value; Toolbar.IsEnabled = !value; Cards.IsEnabled = !value; LayoutHost.IsEnabled = !value; ProfileButtons.IsEnabled = !value;
        QuickStateChanged?.Invoke(this, EventArgs.Empty);
        if (!value && pending && !closing) { pending = false; debounce.Start(); }
    }
    public async Task RefreshAsync()
    {
        if (busy) { pending = true; return; }
        SetBusy(true); Status.Text = "Reading displays and supported hardware controls…";
        try
        {
            displays = await service.ReadAsync();
            await RefreshLayoutAsync();
            if (closing) return;
            Render(); Status.Text = displays.Any(d => d.Brightness?.IsStale == true || d.Contrast?.IsStale == true) ? "Some controls did not respond. Last-known values are marked and disabled; refresh to retry." : "Ready · Changes apply only when you choose Apply or Preview.";
        }
        catch (Exception ex) { Summary.Text = displays.Count == 0 ? "Display discovery failed" : "Showing previous display information"; Status.Text = "Could not refresh displays: " + ex.Message; }
        finally { SetBusy(false); }
    }
    private static TextBlock Text(string value, double size = 14, string color = "Ink")
    {
        var text = new TextBlock { Text = value, FontSize = size, TextWrapping = TextWrapping.Wrap };
        text.SetResourceReference(TextBlock.ForegroundProperty, color); return text;
    }
    internal async Task ApplyProfileAsync(LightingProfile profile)
    {
        if (busy) return;
        SetBusy(true); ProfileDetails.Visibility = Visibility.Collapsed; ProfileStatus.Visibility = Visibility.Visible;
        ProfileStatus.Text = Status.Text = $"Reading current controls before applying {profile.Name.ToLowerInvariant()}…";
        try
        {
            var profiles = new LightingProfiles(service.ReadAsync, service.SetAsync);
            var result = await profiles.ApplyAsync(profile, new Progress<string>(message =>
            {
                if (!closing) ProfileStatus.Text = Status.Text = message;
            }), profileCancellation.Token);
            if (closing) return;
            if (result.ReadBack != null) { displays = result.ReadBack; Render(); await RefreshLayoutAsync(); }
            else { displays = displays.Select(d => d with { Brightness = d.Brightness is { } b ? b with { IsStale = true } : null, Contrast = d.Contrast is { } c ? c with { IsStale = true } : null }).ToList(); Render(); }
            ProfileStatus.Text = Status.Text = result.Controls.Count == 0 ? "No connected monitors were found." : $"{profile.Name} · {result.Summary}";
            ProfileReport.Text = string.Join(Environment.NewLine, result.Controls.Select(c => $"Display {c.Display.Number} · {(c.Contrast ? "Contrast" : "Brightness")} · {c.Status}: {c.Detail}"));
            if (result.ReadError != null) ProfileReport.Text += Environment.NewLine + "Read-back failed: " + result.ReadError;
            ProfileDetails.Visibility = Visibility.Visible;
        }
        catch (OperationCanceledException) { if (!closing) ProfileStatus.Text = "Profile application cancelled; completed changes remain applied."; }
        catch (Exception ex) { if (!closing) ProfileStatus.Text = Status.Text = "Could not apply profile: " + ex.Message; }
        finally { SetBusy(false); }
    }
    private void Render()
    {
        if (warmEnabled) UpdateWarmFilter();
        Cards.Children.Clear();
        Summary.Text = $"{displays.Count} connected display{(displays.Count == 1 ? "" : "s")}";
        if (displays.Count == 0) Cards.Children.Add(Text("No active displays were found. Connect a screen and refresh.", 16, "Muted"));
        foreach (var display in displays)
        {
            var content = new StackPanel();
            content.Children.Add(Text($"DISPLAY {display.Number:00}  {(display.Primary ? "· PRIMARY" : "· EXTENDED")}", 11, "Accent"));
            var title = Text(display.Name, 17); title.Margin = new(0, 6, 0, 6); title.FontWeight = FontWeights.SemiBold; content.Children.Add(title);
            content.Children.Add(Text($"{display.Width} × {display.Height}   /   {(display.Hertz > 1 ? $"{display.Hertz} Hz" : "Refresh rate unavailable")}", 14, "Muted"));
            content.Children.Add(ModeControl(display));
            var controls = new Grid { Margin = new(0, 14, 0, 12) };
            controls.ColumnDefinitions.Add(new()); controls.ColumnDefinitions.Add(new());
            var brightness = Control(display, false); brightness.Margin = new(0, 0, 14, 0);
            var contrast = Control(display, true); Grid.SetColumn(contrast, 1);
            controls.Children.Add(brightness); controls.Children.Add(contrast); content.Children.Add(controls);
            content.Children.Add(Text(display.Note, 12, "Muted"));
            var card = new Border { Child = content, Padding = new(16), CornerRadius = new(14), BorderThickness = new(1), Margin = new(0, 0, 12, 12) };
            card.SetResourceReference(Border.BackgroundProperty, "Card"); card.SetResourceReference(Border.BorderBrushProperty, "Line"); Cards.Children.Add(card);
            SizeCards();
        }
    }
    private StackPanel ModeControl(Display display)
    {
        var panel = new StackPanel { Margin = new(0, 14, 0, 0) };
        panel.Children.Add(Text("Display mode", 13));
        List<DisplayMode> modes;
        try { modes = DisplayModes.Read(display.Device); }
        catch (Exception ex) { panel.Children.Add(Text(ex.Message, 12, "Muted")); return panel; }
        if (modes.Count == 0) { panel.Children.Add(Text("No compatible display modes available.", 12, "Muted")); return panel; }
        var row = new WrapPanel { Margin = new(0, 6, 0, 6) };
        var resolution = new ComboBox { MinWidth = 132, Margin = new(0, 0, 12, 8) };
        var rate = new ComboBox { MinWidth = 90, Margin = new(0, 0, 12, 8) };
        System.Windows.Automation.AutomationProperties.SetName(resolution, $"Display {display.Number} resolution");
        System.Windows.Automation.AutomationProperties.SetName(rate, $"Display {display.Number} refresh rate");
        var apply = new Button { Content = "Preview", IsEnabled = false, Margin = new(0, 0, 0, 8) };
        var sizes = modes.Select(m => (m.Width, m.Height)).Distinct().ToList();
        resolution.ItemsSource = sizes.Select(s => new ComboBoxItem
        {
            Content = $"{s.Width} × {s.Height}"
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
                rate.Items.Add(new ComboBoxItem { Content = $"{m.Hertz} Hz", Tag = m.Hertz });
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
        panel.Children.Add(Text("Orientation", 13));
        var rotationRow = new WrapPanel { Margin = new(0, 6, 0, 6) };
        var orientation = new ComboBox { MinWidth = 170, Margin = new(0, 0, 12, 8) };
        System.Windows.Automation.AutomationProperties.SetName(orientation, $"Display {display.Number} orientation");
        var active = new Native.DevMode { Width = (uint)display.Width, Height = (uint)display.Height, Frequency = display.Hertz, Orientation = display.Orientation };
        for (uint value = 0; value < 4; value++)
        {
            var rotated = DisplayModes.Describe(DisplayModes.Rotate(active, value));
            orientation.Items.Add(new ComboBoxItem { Tag = rotated, Content = rotated.OrientationName });
        }
        orientation.SelectedIndex = (int)display.Orientation;
        var rotate = new Button { Content = "Rotate", IsEnabled = false, Margin = new(0, 0, 0, 8) };
        orientation.SelectionChanged += (_, _) => rotate.IsEnabled = orientation.SelectedItem is ComboBoxItem { Tag: DisplayMode m } && m.Orientation != display.Orientation;
        rotate.Click += async (_, _) => { if (orientation.SelectedItem is ComboBoxItem { Tag: DisplayMode mode }) await PreviewModeAsync(display, mode); };
        rotationRow.Children.Add(orientation); rotationRow.Children.Add(rotate); panel.Children.Add(rotationRow);
        rotate.ToolTip = "Preview orientation using the current resolution and refresh rate.";
        panel.Children.Add(Text("15-second undo protection · Session only", 12, "Muted"));
        return panel;
    }

    private Task PreviewModeAsync(Display display, DisplayMode mode) =>
        PreviewChangeAsync(() => DisplayModes.Apply(display, mode), mode.ToString());

    private readonly DisplayLayout layout = new();
    private LayoutSnapshot? layoutSnapshot;
    private async Task RefreshLayoutAsync()
    {
        try
        {
            var snapshot = await Task.Run(layout.Read);
            if (closing) return;
            if (layoutSnapshot != null && LayoutHost.Content is LayoutEditor && DisplayLayout.SameState(layoutSnapshot, snapshot)) return;
            var screens = snapshot.Screens().Select(s => s with { Number = displays.FirstOrDefault(d => d.Device == s.Device)?.Number ?? s.Number }).ToList();
            layoutSnapshot = snapshot;
            if (screens.Count < 2) { LayoutHost.Content = Text("Connect a second extended display to arrange your workspace.", 14, "Muted"); return; }
            var editor = new LayoutEditor(screens);
            editor.PreviewRequested += async (_, _) =>
            {
                var requested = editor.Placements;
                await PreviewChangeAsync(() => layout.Apply(snapshot, requested), "this screen layout");
                // A completed preview resets its draft, even when a rollback restored the same topology.
                layoutSnapshot = null;
                SetBusy(true);
                try { await RefreshLayoutAsync(); } finally { SetBusy(false); }
            };
            LayoutHost.Content = editor;
        }
        catch (Exception ex) { layoutSnapshot = null; LayoutHost.Content = Text("Layout unavailable: " + ex.Message, 13, "Muted"); }
    }

    private void SizeSections()
    {
        bool compact = TopSections.ActualWidth < 1000;
        TopSections.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(280);
        Grid.SetColumn(LightingPanel, compact ? 0 : 1);
        Grid.SetRow(LightingPanel, compact ? 1 : 0);
        ArrangementPanel.Margin = compact ? new(0, 0, 0, 12) : new(0, 0, 16, 0);
        ProfileButtons.Columns = compact ? 3 : 1;
        foreach (var button in ProfileButtons.Children.OfType<Button>())
            button.Margin = compact ? new(0, 0, 8, 0) : new(0, 0, 0, 8);
    }
    private void SizeCards()
    {
        double available = Math.Max(320, Cards.ActualWidth);
        int columns = Math.Clamp((int)(available / 360), 1, 3);
        double width = available / columns - 12;
        foreach (var card in Cards.Children.OfType<Border>()) card.Width = width;
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
        if (exitRequested) { Close(); return; }
        if (hideAfterPreview) { hideAfterPreview = false; CloseLabels(); Hide(); }
        await RefreshAsync();
        debounce.Stop(); pending = false;
        Status.Text = result;
    }

    internal StackPanel Control(Display display, bool contrast, bool compact = false)
    {
        string name = contrast ? "Contrast" : "Brightness";
        Level? level = contrast ? display.Contrast : display.Brightness;
        var panel = new StackPanel();
        var caption = Text(name, 13); caption.FontWeight = FontWeights.SemiBold; panel.Children.Add(caption);
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
            await RefreshLayoutAsync();
                if (!closing) { Render(); Status.Text = $"{name} command sent. Display values have been read back."; }
            }
            catch (Exception ex) { Status.Text = $"Could not update {name.ToLowerInvariant()}: {ex.Message} Refresh before trying again."; }
            finally { SetBusy(false); }
        };
        if (compact)
        {
            panel.Children.Clear();
            var row = new Grid();
            row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            row.RowDefinitions.Add(new() { Height = GridLength.Auto }); row.RowDefinitions.Add(new() { Height = GridLength.Auto });
            row.Children.Add(caption); Grid.SetColumn(value, 1); row.Children.Add(value);
            slider.Margin = new(0, 6, 10, 2); Grid.SetRow(slider, 1); row.Children.Add(slider);
            apply.Content = "Apply"; apply.Padding = new(8, 4, 8, 4); apply.Margin = new(0, 6, 0, 0);
            Grid.SetColumn(apply, 1); Grid.SetRow(apply, 1); row.Children.Add(apply); panel.Children.Add(row);
        }
        else { panel.Children.Add(slider); panel.Children.Add(value); panel.Children.Add(apply); }
        if (level.IsStale) panel.Children.Add(Text("Monitor did not respond. Refresh to reconnect.", 12, "Muted"));
        return panel;
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private void Warm_Click(object sender, RoutedEventArgs e)
        => ToggleEyeComfort();
    internal void ToggleEyeComfort()
    {
        warmEnabled = !warmEnabled;
        UpdateWarmFilter();
    }
    private void WarmStrength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (warmEnabled) UpdateWarmFilter();
    }
    private void UpdateWarmFilter()
    {
        try
        {
            if (warmEnabled) warmFilter.Show(displays, (int)WarmStrength.Value);
            else warmFilter.Dispose();
            WarmButton.Content = warmEnabled ? "Eye comfort · On" : "Eye comfort · Off";
        }
        catch (Exception ex)
        {
            warmEnabled = false; warmFilter.Dispose(); WarmButton.Content = "Eye comfort · Off";
            Status.Text = "Could not apply warm filter: " + ex.Message;
        }
        QuickStateChanged?.Invoke(this, EventArgs.Empty);
    }
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
