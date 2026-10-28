using Microsoft.Playwright;

namespace WarningSystemTest.Pages;

public sealed class HomePage(IPage page) : WarningPage(page)
{
    public ILocator TaskRows => Page.Locator("#taskListTable tr.task-list-row");
    public ILocator NewIssue => Page.Locator("#btnRequestWarning");
    public ILocator NewDiscussion => Page.Locator("#btnAbsenceDiscussion");
    public ILocator DiscussionDialog => Page.Locator("#absenceDiscussionModal");
    public ILocator DiscussionForm => Page.Locator("#absenceDiscussionForm");
    public ILocator ReportRows => Page.Locator("#reportsTable tr.report-row:visible");
    public Task<int> OpenAsync() => OpenRouteAsync("/");
    public Task SearchTasksAsync(string text) => Page.Locator("#taskListSearch").FillAsync(text);
    public ILocator TaskFor(string employee, string status) => TaskRows
        .Filter(new() { Has = Page.Locator($"td:first-child:text-is('{status}')") })
        .Filter(new() { Has = Page.Locator($"td:nth-child(2):text-is('{employee}')") });
    public Task ClickNewIssueAsync() => NewIssue.ClickAsync();
    public Task OpenDiscussionAsync() => NewDiscussion.ClickAsync();
    public async Task<IResponse> CreateDiscussionAsync(string employeeId, int subtypeId, string date, string description)
    {
        await OpenDiscussionAsync();
        await Page.Locator("#absenceEmployeeId").SelectOptionAsync(employeeId);
        await Page.Locator("#absenceSubType").SelectOptionAsync(subtypeId.ToString());
        await Page.Locator("#absenceDates").FillAsync(date);
        await Page.Locator("#absenceDescription").FillAsync(description);
        return await SaveDiscussionAsync();
    }
    public Task<IResponse> SaveDiscussionAsync() =>
        PostByClickAsync(Page.Locator("#saveAbsenceDiscussion"), "/Home/CreateAbsenceDiscussion");
    public Task ClickSaveDiscussionAsync() => Page.Locator("#saveAbsenceDiscussion").ClickAsync();
    public async Task OpenReportsAsync()
    {
        await OpenAsync();
        await Page.Locator("#reports-tab").ClickAsync();
        await Page.Locator("[data-bs-target='#reportIssueDetailsCollapse']").ClickAsync();
    }
}
