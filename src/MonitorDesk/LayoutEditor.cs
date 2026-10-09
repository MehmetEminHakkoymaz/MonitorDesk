using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MonitorDesk.Services;

namespace MonitorDesk;

internal sealed class LayoutEditor : UserControl
{
    private readonly Canvas surface = new() { Background = Brushes.Transparent, ClipToBounds = true, Height = 190, ToolTip = L.Get("Drag screens to match your desk. Arrow keys to adjust; Shift for precision.") };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(0, 8, 0, 8), FontSize = 12 };
    private readonly Button preview = new() { Content = L.Get("Preview layout"), Margin = new(12, 0, 0, 0) };
    private readonly List<ScreenPlacement> original;
    private List<ScreenPlacement> screens;
    private double scale = 0.1, offsetX, offsetY;
    private Point start;
    private ScreenPlacement? dragging;
    private Border? draggedTile;
    internal event EventHandler? PreviewRequested;
    internal IReadOnlyList<ScreenPlacement> Placements => LayoutGeometry.Normalize(screens);

    internal LayoutEditor(IEnumerable<ScreenPlacement> placements)
    {
        original = placements.ToList(); screens = original.ToList();


        SetResourceReference(BackgroundProperty, "Card");
        var root = new Grid();
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var heading = new StackPanel { Margin = new(0, 0, 0, 10) };
        heading.Children.Add(new TextBlock { Text = L.Get("Arrange your screens"), FontSize = 17, FontWeight = FontWeights.SemiBold });
        root.Children.Add(heading);
        var frame = new Border { Child = surface, BorderThickness = new(1), CornerRadius = new(10) };
        frame.SetResourceReference(Border.BorderBrushProperty, "Line"); frame.SetResourceReference(BackgroundProperty, "Page");
        Grid.SetRow(frame, 1); root.Children.Add(frame);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var reset = new Button { Content = L.Get("Reset draft"), Margin = new(0, 0, 12, 0) };

        reset.Click += (_, _) => { screens = original.ToList(); Draw(); };

        preview.Click += (_, _) => { if (LayoutGeometry.Validate(screens) == null) PreviewRequested?.Invoke(this, EventArgs.Empty); };
        buttons.Children.Add(reset); buttons.Children.Add(preview);
        var footer = new Grid { Margin = new(0, 8, 0, 0) };
        footer.ColumnDefinitions.Add(new()); footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        status.Margin = new(0, 0, 16, 0); status.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(status); Grid.SetColumn(buttons, 1); footer.Children.Add(buttons);
        Grid.SetRow(footer, 2); root.Children.Add(footer); Content = root;
        surface.SizeChanged += (_, _) => { if (dragging == null) Draw(); };
        Loaded += (_, _) => Draw();
    }

    private void Draw(string? focusDevice = null)
    {
        surface.Children.Clear();
        if (screens.Count == 0) return;
        double left = screens.Min(s => s.X), top = screens.Min(s => s.Y);
        double width = screens.Max(s => s.Right) - left, height = screens.Max(s => s.Bottom) - top;
        scale = Math.Min(Math.Max(1, surface.ActualWidth - 100) / width, Math.Max(1, surface.ActualHeight - 28) / height);
        offsetX = (surface.ActualWidth - width * scale) / 2 - left * scale;
        offsetY = (surface.ActualHeight - height * scale) / 2 - top * scale;
        foreach (var screen in screens)
        {
            var label = new TextBlock { Text = $"{screen.Number}" + (screen.Primary ? L.Get("\nPrimary") : ""), FontSize = 16, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var tile = new Border { Child = label, Width = Math.Max(12, screen.Width * scale), Height = Math.Max(12, screen.Height * scale), BorderThickness = new(2), CornerRadius = new(6), Cursor = Cursors.SizeAll, Focusable = true,
                ToolTip = L.Format("Display {0}: {1} × {2}; position {3}, {4}", screen.Number, screen.Width, screen.Height, screen.X, screen.Y) };
            tile.SetResourceReference(BackgroundProperty, "Card"); tile.SetResourceReference(Border.BorderBrushProperty, "Accent");
            System.Windows.Automation.AutomationProperties.SetName(tile, L.Format("Display {0}{1}. Use arrow keys to move.", screen.Number, screen.Primary ? L.Get(", primary") : ""));
            Canvas.SetLeft(tile, offsetX + screen.X * scale); Canvas.SetTop(tile, offsetY + screen.Y * scale);
            tile.GotKeyboardFocus += (_, _) => tile.BorderThickness = new(4);
            tile.LostKeyboardFocus += (_, _) => tile.BorderThickness = new(2);
            tile.MouseLeftButtonDown += (_, e) =>
            {
                tile.Focus(); dragging = screens.Single(s => s.Device == screen.Device); draggedTile = tile;
                start = e.GetPosition(surface); tile.CaptureMouse(); Panel.SetZIndex(tile, 10); e.Handled = true;
            };
            tile.MouseMove += (_, e) =>
            {
                if (dragging == null || draggedTile != tile || !tile.IsMouseCaptured) return;
                var point = e.GetPosition(surface);
                var moved = dragging with { X = (int)Math.Clamp(dragging.X + (point.X - start.X) / scale, -90000, 90000), Y = (int)Math.Clamp(dragging.Y + (point.Y - start.Y) / scale, -90000, 90000) };
                moved = LayoutGeometry.Snap(moved, screens, (int)Math.Ceiling(14 / scale));
                screens[screens.FindIndex(s => s.Device == moved.Device)] = moved;
                Canvas.SetLeft(tile, offsetX + moved.X * scale); Canvas.SetTop(tile, offsetY + moved.Y * scale);
                UpdateStatus();
            };
            tile.MouseLeftButtonUp += (_, e) =>
            {
                if (draggedTile != tile) return;
                dragging = null; draggedTile = null; tile.ReleaseMouseCapture(); Draw(screen.Device); e.Handled = true;
            };
            tile.LostMouseCapture += (_, _) =>
            {
                if (draggedTile != tile) return;
                dragging = null; draggedTile = null; Draw(screen.Device);
            };
            tile.KeyDown += (_, e) =>
            {
                int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 1 : 10;
                int dx = e.Key == Key.Left ? -step : e.Key == Key.Right ? step : 0;
                int dy = e.Key == Key.Up ? -step : e.Key == Key.Down ? step : 0;
                if (dx == 0 && dy == 0) return;
                int index = screens.FindIndex(s => s.Device == screen.Device);
                screens[index] = screens[index] with { X = Math.Clamp(screens[index].X + dx, -90000, 90000), Y = Math.Clamp(screens[index].Y + dy, -90000, 90000) };
                Draw(screen.Device); e.Handled = true;
            };
            surface.Children.Add(tile);
            if (focusDevice == screen.Device) tile.Focus();
        }
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        string? error = LayoutGeometry.Validate(screens);
        bool changed = error == null && !LayoutGeometry.Normalize(screens).SequenceEqual(LayoutGeometry.Normalize(original));
        status.Text = error != null ? L.Message(error) : (changed ? L.Get("Ready · Confirm within 15 seconds to keep for this session.") : L.Get("Drag to arrange. Preview to apply."));
        preview.IsEnabled = error == null && changed;
    }
}
