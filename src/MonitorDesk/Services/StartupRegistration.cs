using Microsoft.Win32;

namespace MonitorDesk.Services;

// Current-user Run entry; enabling is only performed by the settings checkbox.
internal sealed class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MonitorDesk";
    private readonly Func<string?> read;
    private readonly Action<string?> write;
    internal string Command { get; }
    internal StartupRegistration(string executable, Func<string?>? read = null, Action<string?>? write = null)
    {
        if (!System.IO.Path.IsPathFullyQualified(executable) || executable.Contains('"') ||
            !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Expected an absolute EXE path.", nameof(executable));
        Command = $"\"{executable}\" --startup --tray";
        this.read = read ?? ReadCommand;
        this.write = write ?? WriteCommand;
    }
    internal bool Enabled => string.Equals(read(), Command, StringComparison.OrdinalIgnoreCase);
    internal void SetEnabled(bool enabled)
    {
        if (enabled) write(Command);
        else if (Enabled) write(null);
    }
    private static string? ReadCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) as string;
    }
    private static void WriteCommand(string? command)
    {
        if (command == null)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        else
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            key.SetValue(ValueName, command, RegistryValueKind.String);
        }
    }
}
