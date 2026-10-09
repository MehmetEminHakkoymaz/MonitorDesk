using System.Windows;
using Forms = System.Windows.Forms;

namespace MonitorDesk;

internal sealed class TrayIcon : IDisposable
{
    private readonly MainWindow window;
    private readonly Forms.ContextMenuStrip menu = new();
    private readonly Forms.NotifyIcon icon;

    internal TrayIcon(MainWindow window)
    {
        this.window = window;
        menu.Items.Add("Aç", null, (_, _) => ShowWindow());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Çıkış", null, (_, _) => window.Dispatcher.Invoke(window.ExitApplication));
        icon = new Forms.NotifyIcon { Text = "MonitorDesk", Icon = System.Drawing.SystemIcons.Application, ContextMenuStrip = menu, Visible = true };
        icon.DoubleClick += (_, _) => ShowWindow();
    }

    internal void ShowWindow() => window.Dispatcher.Invoke(() =>
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.Activate();
    });

    public void Dispose()
    {
        icon.Visible = false; icon.Dispose(); menu.Dispose();
    }
}
