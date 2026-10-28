using Microsoft.Playwright;

namespace WarningSystemTest.Pages;

public sealed class WarningWizardPage(IPage page) : WarningPage(page)
{
    public ILocator Employee => Page.Locator("#EmployeeId");
    public ILocator SubmitToLegal => Page.Locator("#btnMarkNew");
    public Task<int> OpenAsync(long? issueId = null) => OpenRouteAsync(
        issueId is null ? "/Transgression/Index" : $"/Transgression/Index/{issueId}");
    public Task SelectEmployeeAsync(string id) => Employee.SelectOptionAsync(id);
    public Task SelectCategoryAsync(int id) => Page.Locator($"input[name=CategoryIds][value='{id}']").CheckAsync();
    public Task OpenStepAsync(int step) => Page.Locator($".ws-step[data-step='{step}']").ClickAsync();
    public Task<IResponse> SaveDraftAsync() => PostByClickAsync(
        Page.Locator("#frmTransgression button[type=submit]"), "/Transgression/CreateTransgression");
    public Task ClickSaveDraftAsync() => Page.Locator("#frmTransgression button[type=submit]").ClickAsync();
    public async Task<long> IssueIdAsync() => long.Parse(await Page.Locator("#frmTransgression input[name=WarningId]").InputValueAsync());
    public Task<IResponse> SubmitAsync() => PostByClickAsync(SubmitToLegal, "/Transgression/UpdateStatus");
}
