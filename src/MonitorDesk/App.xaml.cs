using System.IO;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MonitorDesk.Services;

namespace MonitorDesk;
public partial class App : Application
{
    private TrayIcon? tray;
    private SingleInstance? instance;
    protected override void OnExit(ExitEventArgs e)
    {
        tray?.Dispose();
        instance?.Dispose();
        base.OnExit(e);
    }
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 3 && e.Args[0] == "--instance-probe")
        {
            using var probeInstance = new SingleInstance(e.Args[1]);
            File.WriteAllText(e.Args[2], probeInstance.Acquired ? "Acquired" : "Already running");
            Shutdown();
            return;
        }
        bool diagnostic = e.Args.Length >= 2 && e.Args[0] is "--probe" or "--snapshot" or "--layout-snapshot" or "--tray-snapshot" or "--tray-check";
        if (!diagnostic)
        {
            instance = new SingleInstance();
            if (!instance.Acquired)
            {
                bool turkish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr";
                string title = turkish ? "MonitorDesk zaten açık" : "MonitorDesk is already running";
                string message = turkish
                    ? "MonitorDesk zaten açık ve sistem tepsisinde çalışıyor.\n\nSistem tepsisindeki MonitorDesk simgesinden paneli açabilir veya simgeye çift tıklayarak ana pencereye dönebilirsin."
                    : "MonitorDesk is already running in the system tray.\n\nClick its tray icon to open the quick controls, or double-click it to restore the main window.";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
        }
        if (e.Args.Length == 2 && e.Args[0] == "--probe")
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                var displays = await new DisplayService().ReadAsync();
                await File.WriteAllTextAsync(e.Args[1], JsonSerializer.Serialize(displays, new JsonSerializerOptions { WriteIndented = true }));
                Shutdown(0);
            }
            catch (Exception ex) { await File.WriteAllTextAsync(e.Args[1], JsonSerializer.Serialize(new { Error = ex.Message })); Shutdown(1); }
            return;
        }
        var window = new MainWindow(); MainWindow = window;
        bool snapshot = e.Args.Length >= 2 && (e.Args[0] == "--snapshot" || e.Args[0] == "--layout-snapshot" || e.Args[0] == "--tray-snapshot");
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        if (!snapshot)
        {
            try { tray = new TrayIcon(window); window.HideOnClose = true; }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("Tray icon unavailable; closing will exit: {0}", ex.Message); }
        }
        if (snapshot && e.Args.Length >= 4 && int.TryParse(e.Args[2], out int width) && int.TryParse(e.Args[3], out int height))
        { window.Width = Math.Clamp(width, 680, 2560); window.Height = Math.Clamp(height, 520, 1600); }
        window.Show();
        if (e.Args.Length == 2 && e.Args[0] == "--tray-check")
        {
            await window.InitialRead.Task;
            try
            {
                if (tray == null) throw new InvalidOperationException("Tray initialization failed.");
                window.Close();
                if (window.IsVisible) throw new InvalidOperationException("Close did not hide the window.");
                tray.TogglePanel();
                if (!tray.IsPanelVisible) throw new InvalidOperationException("Quick panel did not open.");
                tray.TogglePanel();
                if (tray.IsPanelVisible) throw new InvalidOperationException("Quick panel did not hide.");
                tray.ShowWindow();
                if (!window.IsVisible) throw new InvalidOperationException("Tray did not restore the window.");
                await File.WriteAllTextAsync(e.Args[1], "PASS close hides the window; quick panel opens and hides; tray restores the main window; explicit exit requested.");
                window.ExitApplication();
            }
            catch (Exception ex) { await File.WriteAllTextAsync(e.Args[1], "FAIL " + ex.Message); Shutdown(1); }
            return;
        }
        if (snapshot)
        {
            // Render the actual WPF layout with real read-only display data for local QA.
            await window.InitialRead.Task;
            if (e.Args.Contains("--light")) window.Toolbar.Children.OfType<System.Windows.Controls.Button>().Last().RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (e.Args[0] == "--tray-snapshot")
            {
                window.Hide();
                var quick = new TrayPanel(window); quick.Show();
                await quick.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                SaveSnapshot(quick, e.Args[1]); quick.Close();
            }
            else SaveSnapshot(window, e.Args[1]);
            Shutdown();
        }
    }
    private static void SaveSnapshot(Window window, string path)
    {
        window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}

