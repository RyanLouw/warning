using Microsoft.Playwright;

namespace WarningSystemTest.Pages;

// Page objects contain browser interactions. Business expectations stay in tests.
public abstract class WarningPage(IPage page)
{
    protected IPage Page { get; } = page;
    public Task ReloadAsync() => Page.ReloadAsync();
    public async Task<int> OpenRouteAsync(string route) => (await Page.GotoAsync(route))!.Status;
    protected Task<IResponse> PostByClickAsync(ILocator button, string action) =>
        Page.RunAndWaitForResponseAsync(() => button.ClickAsync(),
            response => response.Request.Method == "POST" && response.Url.Contains(action));
    public Task<string> AntiforgeryTokenAsync() =>
        Page.Locator("input[name=__RequestVerificationToken]").First.InputValueAsync();
}
