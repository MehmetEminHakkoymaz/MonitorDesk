using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.IO;
using MonitorDesk.Services;

internal static class InstallerDownloadChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        byte[] data = [1, 2, 3, 4];
        string hash = Convert.ToHexString(SHA256.HashData(data));
        string file = "Monilivo-Setup-0.23.0-win-x64.exe";
        var package = new UpdatePackage(file, new("https://github.com/test/setup.exe"), new("https://github.com/test/setup.sha256"), data.Length);
        string root = Path.Combine(Path.GetTempPath(), "Monilivo-download-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var client = new HttpClient(new Handler(data, hash + "  " + file));
            string path = await new InstallerDownload(client).DownloadAsync(package, Path.Combine(root, "good"), null, default);
            check(File.ReadAllBytes(path).SequenceEqual(data) && !File.Exists(path + ".partial"), "A verified update is finalized only after matching size and SHA-256");
            using var corrupt = new HttpClient(new Handler(data, new string('0', 64) + "  " + file));
            try { await new InstallerDownload(corrupt).DownloadAsync(package, Path.Combine(root, "bad"), null, default); throw new Exception("Corrupt download accepted"); }
            catch (InvalidDataException) { check(Directory.GetFiles(Path.Combine(root, "bad")).Length == 0, "Checksum mismatch removes partial downloads and never produces an executable"); }
            try { await new InstallerDownload(client).DownloadAsync(package with { Size = 3 }, Path.Combine(root, "size"), null, default); throw new Exception("Wrong size accepted"); }
            catch (InvalidDataException) { check(Directory.GetFiles(Path.Combine(root, "size")).Length == 0, "Oversized response is rejected and cleaned up"); }
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            try { await new InstallerDownload(client).DownloadAsync(package, Path.Combine(root, "cancel"), null, cancelled.Token); throw new Exception("Cancellation ignored"); }
            catch (OperationCanceledException) { check(true, "Download cancellation does not create a runnable installer"); }
            try { InstallerDownload.ParseChecksum(hash + "  other.exe", file); throw new Exception("Wrong filename accepted"); }
            catch (InvalidDataException) { check(true, "Checksum must name the exact installer asset"); }
        }
        finally
        {
            foreach (string directory in Directory.GetDirectories(root))
            { foreach (string filePath in Directory.GetFiles(directory)) File.Delete(filePath); Directory.Delete(directory); }
            Directory.Delete(root);
        }
    }
    private sealed class Handler(byte[] data, string checksum) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = request.RequestUri!.AbsolutePath.EndsWith(".sha256") ? new StringContent(checksum) : new ByteArrayContent(data) });
    }
}
