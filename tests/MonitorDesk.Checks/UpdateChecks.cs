using System.Net;
using System.Net.Http;
using MonitorDesk.Services;

internal static class UpdateChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        check(UpdateChecker.TryVersion("v0.20.0", out var parsed) && parsed == new Version(0, 20, 0), "Published release tags parse as numeric versions");
        check(!UpdateChecker.TryVersion("v0.20.0-beta", out _) && !UpdateChecker.TryVersion("bad", out _), "Unrecognized and prerelease tags cannot trigger updates");
        check(UpdateChecker.IsNewer(new(0, 20, 0), new(0, 19, 2, 0)) && !UpdateChecker.IsNewer(new(0, 20, 0), new(0, 20, 0, 0)), "Version comparison ignores the assembly revision and compares numerically");
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK, "{\"tag_name\":\"v0.21.0\",\"draft\":false,\"prerelease\":false,\"html_url\":\"https://untrusted.example/\"}"));
        var release = await new UpdateChecker(client).CheckAsync(default);
        check(release?.Version == new Version(0, 21, 0) && release.Page.AbsoluteUri == UpdateChecker.ReleasesPage + "/tag/v0.21.0", "A new release opens only our repository's download page");
        check(release?.Package == null, "A release without a matching setup pair retains only the download-page action");
        using var packaged = new HttpClient(new ResponseHandler(HttpStatusCode.OK, "{\"tag_name\":\"v0.23.0\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"Monilivo-Setup-0.23.0-win-x64.exe\",\"size\":100},{\"name\":\"Monilivo-Setup-0.23.0-win-x64.exe.sha256\",\"size\":100}]}"));
        var update = await new UpdateChecker(packaged).CheckAsync(default);
        check(update?.Package is { Size: 100 } package && package.Download.AbsoluteUri == UpdateChecker.ReleasesPage + "/download/v0.23.0/Monilivo-Setup-0.23.0-win-x64.exe",
            "Installer URLs are constructed from the fixed repository and exact versioned asset pair");
        foreach (string field in new[] { "draft", "prerelease" })
        {
            using var excluded = new HttpClient(new ResponseHandler(HttpStatusCode.OK, "{\"tag_name\":\"v9.0.0\",\"draft\":" + (field == "draft" ? "true" : "false") + ",\"prerelease\":" + (field == "prerelease" ? "true" : "false") + "}"));
            check(await new UpdateChecker(excluded).CheckAsync(default) == null, field + " releases do not produce update notifications");
        }
        using var missing = new HttpClient(new ResponseHandler(HttpStatusCode.NotFound, ""));
        check(await new UpdateChecker(missing).CheckAsync(default) == null, "No published release is handled without a false newer version");
        using var limited = new HttpClient(new ResponseHandler(HttpStatusCode.Forbidden, ""));
        try { await new UpdateChecker(limited).CheckAsync(default); throw new Exception("Rate limit treated as success"); }
        catch (HttpRequestException) { check(true, "HTTP failures are reported rather than interpreted as up to date"); }
    }
    private sealed class ResponseHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Host != "api.github.com" || request.Headers.UserAgent.Count == 0) throw new Exception("Invalid update request");
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) });
        }
    }
}
