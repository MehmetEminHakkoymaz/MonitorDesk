using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace MonitorDesk.Services;

internal sealed class InstallerDownload(HttpClient client)
{
    internal const long MaximumSize = 200 * 1024 * 1024;
    internal async Task<string> DownloadAsync(UpdatePackage package, string directory, IProgress<int>? progress, CancellationToken cancellation)
    {
        if (package.Size is <= 0 or > MaximumSize || Path.GetFileName(package.FileName) != package.FileName)
            throw new InvalidDataException("Invalid package.");
        Directory.CreateDirectory(directory);
        string partial = Path.Combine(directory, package.FileName + ".partial");
        string complete = Path.Combine(directory, package.FileName);
        try
        {
            using var checksumResponse = await client.GetAsync(package.Checksum, HttpCompletionOption.ResponseHeadersRead, cancellation);
            checksumResponse.EnsureSuccessStatusCode();
            await using var checksumStream = await checksumResponse.Content.ReadAsStreamAsync(cancellation);
            var checksumBytes = new byte[4097];
            int checksumLength = 0, read;
            while ((read = await checksumStream.ReadAsync(checksumBytes.AsMemory(checksumLength), cancellation)) != 0)
            { checksumLength += read; if (checksumLength > 4096) throw new InvalidDataException("Checksum too large."); }
            string expected = ParseChecksum(System.Text.Encoding.UTF8.GetString(checksumBytes, 0, checksumLength), package.FileName);
            using var response = await client.GetAsync(package.Download, HttpCompletionOption.ResponseHeadersRead, cancellation);
            response.EnsureSuccessStatusCode();
            await using (var input = await response.Content.ReadAsStreamAsync(cancellation))
            await using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                long total = 0; byte[] buffer = new byte[81920];
                while ((read = await input.ReadAsync(buffer, cancellation)) != 0)
                {
                    total += read;
                    if (total > package.Size || total > MaximumSize) throw new InvalidDataException("Unexpected package size.");
                    hash.AppendData(buffer, 0, read); await output.WriteAsync(buffer.AsMemory(0, read), cancellation);
                    progress?.Report((int)(total * 100 / package.Size));
                }
                if (total != package.Size || !Convert.ToHexString(hash.GetHashAndReset()).Equals(expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Installer verification failed.");
            }
            File.Move(partial, complete); return complete;
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }
    internal static string ParseChecksum(string text, string file)
    {
        var parts = text.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || parts[0].Length != 64 || !parts[0].All(Uri.IsHexDigit) || parts[1].TrimStart('*') != file)
            throw new InvalidDataException("Invalid checksum.");
        return parts[0];
    }
}
