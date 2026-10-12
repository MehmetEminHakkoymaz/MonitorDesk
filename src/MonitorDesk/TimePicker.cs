using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MonitorDesk;

internal sealed class TimePicker : UserControl
{
    private readonly TextBox hour, minute;
    private readonly Popup popup;
    internal int MinuteOfDay
    {
        get { Normalize(hour); Normalize(minute); return int.Parse(hour.Text, CultureInfo.InvariantCulture) * 60 + int.Parse(minute.Text, CultureInfo.InvariantCulture); }
    }

    internal TimePicker(int value)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        hour = CreateField(value / 60, L.Get("Hour"));
        minute = CreateField(value % 60, L.Get("Minute"));
        row.Children.Add(hour);
        row.Children.Add(new TextBlock { Text = ":", FontSize = 22, Margin = new(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(minute);
        var open = new Button { Content = "⌄", Width = 30, Height = 40, Padding = new(0), Margin = new(6, 0, 0, 0), ToolTip = L.Get("Choose time") };
        System.Windows.Automation.AutomationProperties.SetName(open, L.Get("Choose time")); row.Children.Add(open);
        Content = row;
        popup = new Popup { PlacementTarget = this, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true };
        var columns = new Grid { Margin = new(8) };
        columns.ColumnDefinitions.Add(new()); columns.ColumnDefinitions.Add(new());
        AddChoices(columns, hour, 24, 0, L.Get("Hour")); AddChoices(columns, minute, 60, 1, L.Get("Minute"));
        var outline = new Border { Child = columns, CornerRadius = new(10), BorderThickness = new(1) };
        outline.SetResourceReference(Border.BackgroundProperty, "Card"); outline.SetResourceReference(Border.BorderBrushProperty, "Line");
        popup.Child = outline;
        // Popup content has a separate visual tree; carry resource references via the owner.
        Unloaded += (_, _) => popup.IsOpen = false;
        open.Click += (_, _) => popup.IsOpen = !popup.IsOpen;
        WireInput(hour, 23, minute); WireInput(minute, 59, null);
    }

    private static TextBox CreateField(int value, string name)
    {
        var field = new TextBox { Text = value.ToString("00", CultureInfo.InvariantCulture), Width = 44, Height = 40,
            FontSize = 22, BorderThickness = new(0), TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center, Padding = new(2), ToolTip = name, AllowDrop = false };
        field.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "Page");
        field.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "Ink");
        field.SetResourceReference(TextBox.CaretBrushProperty, "Accent");
        System.Windows.Automation.AutomationProperties.SetName(field, name);
        return field;
    }

    private void WireInput(TextBox field, int maximum, TextBox? next)
    {
        field.GotKeyboardFocus += (_, _) => field.SelectAll();
        field.PreviewMouseLeftButtonDown += (_, e) =>
        { if (!field.IsKeyboardFocusWithin) { e.Handled = true; field.Focus(); } };
        field.PreviewTextInput += (_, e) =>
        {
            e.Handled = true;
            if (e.Text == ":" && next != null) { Normalize(field); next.Focus(); next.SelectAll(); return; }
            var text = TimeEntry.Edit(field.Text, field.SelectionStart, field.SelectionLength, e.Text, maximum);
            if (text == null) return;
            int caret = field.SelectionStart + e.Text.Length;
            field.Text = text; field.CaretIndex = caret;
            if (text.Length == 2 && next != null) { next.Focus(); next.SelectAll(); }
        };
        field.LostKeyboardFocus += (_, _) => Normalize(field);
        field.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Up or Key.Down)
            { Step(field, maximum, e.Key == Key.Up ? 1 : -1); e.Handled = true; }
            if (e.Key == Key.Escape && popup.IsOpen) { popup.IsOpen = false; e.Handled = true; }
            if (e.Key == Key.Right && next != null && field.CaretIndex == field.Text.Length)
            { next.Focus(); next.SelectAll(); e.Handled = true; }
        };
        field.PreviewMouseWheel += (_, e) => { Step(field, maximum, e.Delta > 0 ? 1 : -1); e.Handled = true; };
        DataObject.AddPastingHandler(field, (_, e) =>
        {
            e.CancelCommand();
            if (e.DataObject.GetData(DataFormats.UnicodeText) is not string text) return;
            if (TimeEntry.ParseTime(text) is { } value)
            { hour.Text = (value / 60).ToString("00"); minute.Text = (value % 60).ToString("00"); return; }
            var edited = TimeEntry.Edit(field.Text, field.SelectionStart, field.SelectionLength, text, maximum);
            if (edited != null) { field.Text = edited; Normalize(field); }
        });
    }

    private static void Normalize(TextBox field)
        => field.Text = (int.TryParse(field.Text, out var value) ? value : 0).ToString("00", CultureInfo.InvariantCulture);
    private static void Step(TextBox field, int maximum, int direction)
    {
        Normalize(field);
        field.Text = ((int.Parse(field.Text) + direction + maximum + 1) % (maximum + 1)).ToString("00", CultureInfo.InvariantCulture);
        field.SelectAll();
    }
    private void AddChoices(Grid grid, TextBox field, int count, int column, string title)
    {
        var panel = new StackPanel { Width = 70, Margin = new(3) };
        panel.Children.Add(new TextBlock { Text = title, Margin = new(4, 0, 0, 6), FontSize = 12 });
        var options = new StackPanel();
        Button? selected = null;
        for (int index = 0; index < count; index++)
        {
            string text = index.ToString("00", CultureInfo.InvariantCulture);
            var option = new Button { Content = text, Padding = new(4), Margin = new(0, 0, 0, 2), Height = 29 };
            if (field.Text == text) selected = option;
            option.Click += (_, _) => { field.Text = text; if (field == minute) popup.IsOpen = false; else minute.Focus(); };
            options.Children.Add(option);
        }
        var scroll = new ScrollViewer { Content = options, Height = 200, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scroll.Loaded += (_, _) =>
        {
            // Reopen near the current selection rather than always at midnight.
            var target = options.Children.OfType<Button>().FirstOrDefault(b => Equals(b.Content, field.Text)) ?? selected;
            target?.BringIntoView();
        };
        panel.Children.Add(scroll); Grid.SetColumn(panel, column); grid.Children.Add(panel);
    }
}

internal static class TimeEntry
{
    internal static string? Edit(string current, int start, int length, string input, int maximum)
    {
        if (input.Length == 0 || input.Any(c => c is < '0' or > '9')) return null;
        string result = current.Remove(start, length).Insert(start, input);
        return result.Length is > 0 and <= 2 && int.TryParse(result, out int number) && number <= maximum ? result : null;
    }
    internal static int? ParseTime(string input)
    {
        string text = input.Trim();
        if (text.Length == 4 && text.All(c => c is >= '0' and <= '9')) text = text.Insert(2, ":");
        return TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time.Hour * 60 + time.Minute : null;
    }
}
