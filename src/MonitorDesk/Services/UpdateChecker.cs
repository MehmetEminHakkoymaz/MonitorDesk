using System.Net;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace MonitorDesk.Services;

internal record PublishedUpdate(Version Version, Uri Page);

internal sealed class UpdateChecker(HttpClient client)
{
    internal const string ReleasesPage = "https://github.com/MehmetEminHakkoymaz/MonitorDesk/releases";
    internal async Task<PublishedUpdate?> CheckAsync(CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/MehmetEminHakkoymaz/MonitorDesk/releases/latest");
        request.Headers.UserAgent.ParseAdd("Monilivo-UpdateCheck/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await client.SendAsync(request, cancellation);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
        var release = json.RootElement;
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean()) return null;
        string tag = release.GetProperty("tag_name").GetString() ?? "";
        if (!TryVersion(tag, out var version)) throw new InvalidDataException("Unrecognized release version.");
        // Build the URL from our fixed repository, never open a server-provided arbitrary URL.
        return new(version!, new Uri(ReleasesPage + "/tag/" + Uri.EscapeDataString(tag)));
    }
    internal static bool TryVersion(string tag, out Version? version)
    {
        string value = tag.StartsWith('v') ? tag[1..] : tag;
        version = null;
        if (value.Split('.').Length != 3 || value.Any(c => c != '.' && (c is < '0' or > '9'))) return false;
        return Version.TryParse(value, out version);
    }
    internal static bool IsNewer(Version published, Version current)
        => new Version(published.Major, published.Minor, Math.Max(0, published.Build)) >
           new Version(current.Major, current.Minor, Math.Max(0, current.Build));
}
