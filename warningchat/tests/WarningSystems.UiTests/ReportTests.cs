using System.Globalization;
using Microsoft.Playwright;
using WarningSystems.Testing;
using static Microsoft.Playwright.Assertions;

namespace WarningSystems.UiTests;

public class ReportTests(BrowserFixture fixture) : IClassFixture<BrowserFixture>
{
    public static IEnumerable<object[]> Filters()
    {
        yield return ["all", new ReportFilter()];
        foreach (var status in new[] { "Draft", "New", "In Progress", "Pending", "Completed", "Validated", "Added More Information" })
            yield return [$"status-{status.Replace(' ', '-')}", new ReportFilter(Status: status)];
        foreach (var department in new[] { "Sales", "Support" })
            yield return [$"department-{department}", new ReportFilter(Department: department)];
        yield return ["manager", new ReportFilter(Manager: "Test Manager")];
        foreach (var employee in new[] { "Alice Adams", "Bob Brown", "Cara Clark" })
            yield return [$"employee-{employee.Split(' ')[0]}", new ReportFilter(Employee: employee)];
        foreach (var type in new[] { "Invalid", "Warning", "Discussion", "Hearing Notice" })
            yield return [$"type-{type.Replace(' ', '-')}", new ReportFilter(IssueType: type)];
        yield return ["issues-only", new ReportFilter(RecordKind: "issue")];
        yield return ["discussions-only", new ReportFilter(RecordKind: "absence-discussion")];
        yield return ["issued-action", new ReportFilter(LatestAction: "Issued to Employee")];
        yield return ["review-action", new ReportFilter(LatestAction: "Legal review")];
        yield return ["search", new ReportFilter(Search: "Alice")];
        yield return ["search-case-and-whitespace", new ReportFilter(Search: "  aLiCe  ")];
        yield return ["search-no-results", new ReportFilter(Search: "does-not-exist-98765")];
        yield return ["date-start", new ReportFilter(StartDate: "2026-02-01")];
        yield return ["date-end", new ReportFilter(EndDate: "2026-01-04")];
        yield return ["date-range", new ReportFilter(StartDate: "2026-02-01", EndDate: "2026-02-28")];
        yield return ["date-inclusive-boundary", new ReportFilter(StartDate: "2026-01-02", EndDate: "2026-01-02")];
        yield return ["date-no-results", new ReportFilter(StartDate: "2025-01-01", EndDate: "2025-01-01")];
        yield return ["combined", new ReportFilter(Department: "Sales", Employee: "Alice Adams", Status: "Completed", IssueType: "Warning", LatestAction: "Issued to Employee", StartDate: "2026-02-01", EndDate: "2026-02-28")];
        yield return ["combined-empty", new ReportFilter(Department: "Support", IssueType: "Hearing Notice")];
        yield return ["department-discussion", new ReportFilter(Department: "Support", RecordKind: "absence-discussion")];
        yield return ["manager-status", new ReportFilter(Manager: "Test Manager", Status: "New")];
    }

    [Theory]
    [MemberData(nameof(Filters))]
    public Task Filtered_rows_values_and_totals_match_database(string name, ReportFilter filter) =>
        fixture.RunAsync(name, async page =>
        {
            await OpenReportAsync(page);
            await ApplyAsync(page, filter);
            await AssertReportAsync(page, filter);
        });

    [Theory]
    [InlineData("User")]
    [InlineData("Legal")]
    [InlineData("User,Legal")]
    public Task Report_visibility_matches_database_for_each_role(string roles) =>
        fixture.RunAsync($"role-{roles.Replace(',', '-')}", async page =>
        {
            await OpenReportAsync(page);
            await AssertReportAsync(page, new(), roles);
        }, roles);

    [Fact]
    public Task Clear_filters_restores_database_results() => fixture.RunAsync("clear-filters", async page =>
    {
        await OpenReportAsync(page);
        await ApplyAsync(page, new(Department: "Sales", Status: "Completed", Search: "Alice"));
        await AssertReportAsync(page, new(Department: "Sales", Status: "Completed", Search: "Alice"));
        await page.Locator("#btnClearReportFilters").ClickAsync();
        await Expect(page.Locator("#reportSearch")).ToHaveValueAsync("");
        await Expect(page.Locator("#reportDepartment")).ToHaveValueAsync("");
        await Expect(page.Locator("#reportStatus")).ToHaveValueAsync("");
        await AssertReportAsync(page, new());
    });

    [Fact]
    public Task Changing_department_resets_incompatible_employee_filter() => fixture.RunAsync("dependent-filters", async page =>
    {
        await OpenReportAsync(page);
        await page.Locator("#reportDepartment").SelectOptionAsync("Sales");
        await page.Locator("#reportEmployee").SelectOptionAsync("Alice Adams");
        await page.Locator("#reportDepartment").SelectOptionAsync("Support");
        await Expect(page.Locator("#reportEmployee")).ToHaveValueAsync("");
        await Expect(page.Locator("#reportEmployee option")).ToHaveTextAsync(new[] { "All employees", "Bob Brown" });
        await AssertReportAsync(page, new(Department: "Support"));
    });

    internal static async Task OpenReportAsync(IPage page)
    {
        var response = await page.GotoAsync("/");
        Assert.Equal(200, response!.Status);
        await page.Locator("#reports-tab").ClickAsync();
        await page.Locator("[data-bs-target='#reportIssueDetailsCollapse']").ClickAsync();
        await Expect(page.Locator("#reportIssueDetailsCollapse")).ToBeVisibleAsync();
    }

    private static async Task ApplyAsync(IPage page, ReportFilter filter)
    {
        foreach (var (id, value) in new[] {
            ("reportDepartment", filter.Department), ("reportManager", filter.Manager), ("reportEmployee", filter.Employee),
            ("reportStatus", filter.Status), ("reportIssueType", filter.IssueType), ("reportRecordKind", filter.RecordKind), ("reportLatestAction", filter.LatestAction)
        })
        {
            if (value is null) continue;
            // Match label because some static status values use lower case.
            if (id == "reportRecordKind") await page.Locator($"#{id}").SelectOptionAsync(value);
            else await page.Locator($"#{id}").SelectOptionAsync(new SelectOptionValue { Label = value });
        }
        foreach (var (id, value) in new[] { ("reportSearch", filter.Search), ("reportStartDate", filter.StartDate), ("reportEndDate", filter.EndDate) })
            if (value is not null) await page.Locator($"#{id}").FillAsync(value);
    }

    private async Task AssertReportAsync(IPage page, ReportFilter filter, string roles = "User,Legal")
    {
        var expected = await ReportOracle.QueryAsync(fixture.App, filter, roles);
        var visible = page.Locator("#reportsTable tr.report-row:visible");
        await Expect(visible).ToHaveCountAsync(expected.Count);
        var ids = (await visible.EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.warningId)"))
            .Select(long.Parse).Order().ToArray();
        Assert.Equal(expected.Select(r => r.Id).Order(), ids);
        Assert.DoesNotContain(14L, ids); // Deleted control record must never appear.
        foreach (var record in expected)
        {
            var row = page.Locator($"#reportsTable tr[data-warning-id='{record.Id}']");
            var cells = (await row.Locator("td").AllTextContentsAsync()).Select(t => t.Trim()).ToArray();
            Assert.Equal(new[] {
                record.IsAbsenceDiscussion ? "Discussion" : "Issue", record.Status,
                Dash(record.IssueType), Dash(record.Subtype), record.Category, record.Employee,
                Dash(record.Department), Dash(record.Manager), Dash(record.Creator), record.Created.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
                record.Due?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? "-", Dash(record.LatestAction),
                record.LatestActionDate?.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture) ?? "-"
            }, cells);
        }
        await CountAsync(page, "reportVisibleDetailCount", expected.Count);
        await CountAsync(page, "reportDepartmentCount", expected.Where(r => r.Department != "").Select(r => r.Department).Distinct().Count());
        await CountAsync(page, "reportIssueCount", expected.Count(r => !r.IsAbsenceDiscussion));
        await CountAsync(page, "reportAbsenceDiscussionCount", expected.Count(r => r.IsAbsenceDiscussion));
        await CountAsync(page, "reportDraftCount", expected.Count(r => r.Status == "Draft"));
        await CountAsync(page, "reportInProgressCount", expected.Count(r => r.Status is "New" or "In Progress"));
        await CountAsync(page, "reportCompletedCount", expected.Count(r => r.Status == "Completed"));
        await CountAsync(page, "reportDiscussionCount", expected.Count(r => r.IssueType == "Discussion"));
        await CountAsync(page, "reportWarningTypeCount", expected.Count(r => r.IssueType == "Warning" || r.Status == "Warning"));
        if (expected.Count == 0) await Expect(page.Locator("#reportNoResults")).ToBeVisibleAsync();
        else await Expect(page.Locator("#reportNoResults")).ToBeHiddenAsync();
        var breakdown = page.Locator("#reportDepartmentBreakdown tr");
        if (expected.Count > 0)
        {
            var groups = expected.GroupBy(r => string.IsNullOrWhiteSpace(r.Department) ? "Unknown" : r.Department).OrderBy(g => g.Key).ToArray();
            await Expect(breakdown).ToHaveCountAsync(groups.Length);
            foreach (var group in groups)
            {
                var row = breakdown.Filter(new() { Has = page.Locator("td:first-child", new() { HasTextString = group.Key }) });
                var values = (await row.Locator("td").AllTextContentsAsync()).Select(t => t.Trim()).ToArray();
                Assert.Equal(new[] { group.Key, group.Count().ToString(), group.Count(r => r.Status == "Draft").ToString(),
                    group.Count(r => r.Status == "In Progress").ToString(), group.Count(r => r.Status == "Completed").ToString(),
                    group.Count(r => r.IssueType == "Warning" || r.Status == "Warning").ToString() }, values);
            }
        }
    }

    private static string Dash(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
    private static Task CountAsync(IPage page, string id, int count) =>
        Expect(page.Locator($"#{id}")).ToHaveTextAsync(count.ToString(CultureInfo.InvariantCulture));
}
