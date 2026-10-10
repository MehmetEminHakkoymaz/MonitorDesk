using System.Windows;
using Forms = System.Windows.Forms;

namespace MonitorDesk;

internal sealed class TrayIcon : IDisposable
{
    private readonly MainWindow window;
    private readonly Forms.ContextMenuStrip menu = new();
    private readonly Forms.NotifyIcon icon;
    private readonly System.Drawing.Icon applicationIcon;
    private readonly TrayPanel panel;
    private readonly System.Windows.Threading.DispatcherTimer singleClick = new();
    private System.Drawing.Rectangle panelWorkArea;
    internal bool IsPanelVisible => panel.IsVisible;

    internal TrayIcon(MainWindow window)
    {
        this.window = window;
        panel = new TrayPanel(window);
        panel.SizeChanged += (_, _) => { if (panel.IsVisible) PositionPanel(); };
        singleClick.Interval = TimeSpan.FromMilliseconds(Forms.SystemInformation.DoubleClickTime);
        singleClick.Tick += (_, _) => { singleClick.Stop(); TogglePanel(); };
        menu.Items.Add(L.Get("Open"), null, (_, _) => ShowWindow());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(L.Get("Exit"), null, (_, _) => window.Dispatcher.Invoke(window.ExitApplication));
        using (var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/MonitorDesk.ico")).Stream)
        using (var source = new System.Drawing.Icon(resource, Forms.SystemInformation.SmallIconSize))
            applicationIcon = (System.Drawing.Icon)source.Clone();
        icon = new Forms.NotifyIcon { Text = "Monilivo", Icon = applicationIcon, ContextMenuStrip = menu, Visible = true };
        icon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) { singleClick.Stop(); singleClick.Start(); } };
        icon.MouseDoubleClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) { singleClick.Stop(); ShowWindow(); } };
    }

    internal void ShowWindow() => window.Dispatcher.Invoke(() =>
    {
        singleClick.Stop(); panel.Hide();
        window.Show();
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.Activate();
    });

    internal void TogglePanel()
    {
        if (panel.IsVisible) { panel.Hide(); return; }
        panelWorkArea = Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea;
        double scale = System.Windows.Media.VisualTreeHelper.GetDpi(panel).DpiScaleY;
        panel.MaxHeight = Math.Min(560, (panelWorkArea.Height - 24) / scale);
        panel.Render(); panel.Show(); panel.UpdateLayout();
        PositionPanel();
        panel.Activate();
    }

    private void PositionPanel()
    {
        // Native coordinates keep the popup inside the target monitor on mixed-DPI desktops.
        if (panelWorkArea.Width == 0) return;
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(panel);
        panel.MaxHeight = Math.Min(560, (panelWorkArea.Height - 24) / dpi.DpiScaleY);
        int width = (int)Math.Ceiling(panel.ActualWidth * dpi.DpiScaleX);
        int height = (int)Math.Ceiling(panel.ActualHeight * dpi.DpiScaleY);
        Services.Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(panel).Handle, -1,
            panelWorkArea.Right - width - 12, panelWorkArea.Bottom - height - 12, 0, 0, 0x0011);
    }

    public void Dispose()
    {
        singleClick.Stop(); panel.Close(); icon.Visible = false; icon.Dispose(); applicationIcon.Dispose(); menu.Dispose();
    }
}
