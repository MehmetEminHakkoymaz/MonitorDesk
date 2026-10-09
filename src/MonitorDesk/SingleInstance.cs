using System.Security.Principal;

namespace MonitorDesk;

// Held for the whole app lifetime, including while the main window is hidden.
internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex;
    internal bool Acquired { get; }

    internal SingleInstance(string? name = null)
    {
        name ??= @"Local\MonitorDesk-" + WindowsIdentity.GetCurrent().User!.Value;
        mutex = new Mutex(false, name);
        try { Acquired = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { Acquired = true; }
    }

    public void Dispose()
    {
        if (Acquired) mutex.ReleaseMutex();
        mutex.Dispose();
    }
}
