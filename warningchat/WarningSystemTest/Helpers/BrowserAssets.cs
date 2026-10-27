using Microsoft.Playwright;

namespace WarningSystemTest.Helpers;

internal static class BrowserAssets
{
    private static readonly HashSet<string> Hosts = ["cdn.jsdelivr.net", "cdn.quilljs.com", "cdnjs.cloudflare.com"];
    private static readonly HashSet<string> Names = ["quill.min.js", "quill.snow.css", "tom-select.complete.min.js", "tom-select.bootstrap5.min.css", "bootstrap-datepicker.min.js", "bootstrap-datepicker.min.css"];

    public static async Task RouteAsync(IRoute route, string applicationOrigin)
    {
        var uri = new Uri(route.Request.Url);
        if (uri.GetLeftPart(UriPartial.Authority) == applicationOrigin)
        {
            await route.ContinueAsync();
            return;
        }
        var name = Path.GetFileName(uri.AbsolutePath);
        if (Hosts.Contains(uri.Host) && Names.Contains(name))
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", name);
            await route.FulfillAsync(new() { Status = 200, ContentType = name.EndsWith(".css") ? "text/css" : "application/javascript", Body = await File.ReadAllTextAsync(path) });
            return;
        }
        await route.AbortAsync();
    }
}
