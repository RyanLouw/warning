using Microsoft.Playwright;
using WarningSystemTest.Database;

namespace WarningSystemTest.Pages;

public sealed class ReportPage(IPage page) : WarningPage(page)
{
    public ILocator VisibleRows => Page.Locator("#reportsTable tr.report-row:visible");
    public Task OpenAsync() => new HomePage(Page).OpenReportsAsync();
    public Task ClearFiltersAsync() => Page.Locator("#btnClearReportFilters").ClickAsync();
    public async Task ApplyFiltersAsync(ReportFilter filter)
    {
        foreach (var (id, value) in new[] {
            ("reportDepartment", filter.Department), ("reportManager", filter.Manager), ("reportEmployee", filter.Employee),
            ("reportStatus", filter.Status), ("reportIssueType", filter.IssueType), ("reportRecordKind", filter.RecordKind), ("reportLatestAction", filter.LatestAction)
        })
        {
            if (value is null) continue;
            // Match label because some static status values use lower case.
            if (id == "reportRecordKind") await Page.Locator($"#{id}").SelectOptionAsync(value);
            else await Page.Locator($"#{id}").SelectOptionAsync(new SelectOptionValue { Label = value });
        }
        foreach (var (id, value) in new[] { ("reportSearch", filter.Search), ("reportStartDate", filter.StartDate), ("reportEndDate", filter.EndDate) })
            if (value is not null) await Page.Locator($"#{id}").FillAsync(value);
    }

}
