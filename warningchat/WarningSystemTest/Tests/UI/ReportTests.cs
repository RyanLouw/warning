using System.Globalization;
using Microsoft.Playwright;
using WarningSystemTest.Database;
using WarningSystemTest.Fixtures;
using System.Text.Json;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI;

[Collection("Warning browser")]
[Trait("Category", "UI")]
public class ReportTests(BrowserFixture fixture) : UiTestBase(fixture)
{
    public static IEnumerable<object[]> Filters()
    {
        yield return Case("all", new ReportFilter());
        foreach (var status in new[] { "Draft", "New", "In Progress", "Pending", "Issued", "Validated", "Invalid" })
            yield return Case($"status-{status.Replace(' ', '-')}", new ReportFilter(Status: status));
        foreach (var department in new[] { "Sales", "Support" })
            yield return Case($"department-{department}", new ReportFilter(Department: department));
        yield return Case("manager", new ReportFilter(Manager: "Test User"));
        foreach (var employee in new[] { "UI Alice", "UI Bob", "UI Cara" })
            yield return Case($"employee-{employee.Replace(' ', '-')}", new ReportFilter(Employee: employee));
        foreach (var type in new[] { "Invalid", "Warning", "Discussion", "Hearing" })
            yield return Case($"type-{type.Replace(' ', '-')}", new ReportFilter(IssueType: type));
        yield return Case("issues-only", new ReportFilter(RecordKind: "issue"));
        yield return Case("discussions-only", new ReportFilter(RecordKind: "absence-discussion"));
        yield return Case("issued-action", new ReportFilter(LatestAction: "Issued to Employee"));
        yield return Case("review-action", new ReportFilter(LatestAction: "UI Review"));
        yield return Case("search", new ReportFilter(Search: "Alice"));
        yield return Case("search-case-and-whitespace", new ReportFilter(Search: "  aLiCe  "));
        yield return Case("search-no-results", new ReportFilter(Search: "does-not-exist-98765"));
        yield return Case("date-start", new ReportFilter(StartDate: "2026-02-01"));
        yield return Case("date-end", new ReportFilter(EndDate: "2026-01-04"));
        yield return Case("date-range", new ReportFilter(StartDate: "2026-02-01", EndDate: "2026-02-28"));
        yield return Case("date-inclusive-boundary", new ReportFilter(StartDate: "2026-01-02", EndDate: "2026-01-02"));
        yield return Case("date-no-results", new ReportFilter(StartDate: "2025-01-01", EndDate: "2025-01-01"));
        yield return Case("combined", new ReportFilter(Department: "Sales", Employee: "UI Alice", Status: "Pending", IssueType: "Warning", LatestAction: "UI Review", StartDate: "2026-02-01", EndDate: "2026-02-28"));
        yield return Case("combined-empty", new ReportFilter(Department: "Support", IssueType: "Hearing"));
        yield return Case("department-discussion", new ReportFilter(Department: "Sales", RecordKind: "absence-discussion"));
        yield return Case("manager-status", new ReportFilter(Manager: "Test User", Status: "New"));
    }

    private static object[] Case(string name, ReportFilter filter) => [name, JsonSerializer.Serialize(filter)];

    [Theory]
    [MemberData(nameof(Filters))]
    public Task Filtered_rows_values_and_totals_match_database(string name, string json) =>
        RunAsync(name, async page =>
        {
            var filter = JsonSerializer.Deserialize<ReportFilter>(json)!;
            await OpenReportAsync(page);
            await ApplyAsync(page, filter);
            await AssertReportAsync(page, filter);
        });

    [Theory]
    [InlineData("User")]
    [InlineData("Legal")]
    public Task Report_visibility_matches_database_for_each_role(string roles) =>
        RunAsync($"role-{roles.Replace(',', '-')}", async page =>
        {
            await OpenReportAsync(page);
            var filter = roles == "Legal" ? new ReportFilter(Search: "UI ") : new ReportFilter();
            if (filter.Search is not null) await page.Locator("#reportSearch").FillAsync(filter.Search);
            await AssertReportAsync(page, filter, roles);
        }, roles);

    [Fact]
    public Task Clear_filters_restores_database_results() => RunAsync("clear-filters", async page =>
    {
        await OpenReportAsync(page);
        await ApplyAsync(page, new(Department: "Sales", Status: "Pending", Search: "Alice"));
        await AssertReportAsync(page, new(Department: "Sales", Status: "Pending", Search: "Alice"));
        await page.Locator("#btnClearReportFilters").ClickAsync();
        await Expect(page.Locator("#reportSearch")).ToHaveValueAsync("");
        await Expect(page.Locator("#reportDepartment")).ToHaveValueAsync("");
        await Expect(page.Locator("#reportStatus")).ToHaveValueAsync("");
        await AssertReportAsync(page, new());
    });

    [Fact]
    public Task Changing_department_resets_incompatible_employee_filter() => RunAsync("dependent-filters", async page =>
    {
        await OpenReportAsync(page);
        await page.Locator("#reportDepartment").SelectOptionAsync("Sales");
        await page.Locator("#reportEmployee").SelectOptionAsync("UI Alice");
        await page.Locator("#reportDepartment").SelectOptionAsync("Support");
        await Expect(page.Locator("#reportEmployee")).ToHaveValueAsync("");
        await Expect(page.Locator("#reportEmployee option")).ToHaveTextAsync(new[] { "All employees", "UI Bob" });
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

    private async Task AssertReportAsync(IPage page, ReportFilter filter, string roles = "User")
    {
        var expected = await ReportOracle.QueryAsync(Database, filter, roles);
        var visible = page.Locator("#reportsTable tr.report-row:visible");
        await Expect(visible).ToHaveCountAsync(expected.Count);
        var ids = (await visible.EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.warningId)"))
            .Select(long.Parse).Order().ToArray();
        Assert.Equal(expected.Select(r => Convert.ToInt64(r["WarningId"])).Order(), ids);
        Assert.DoesNotContain(Database.Warnings["Deleted"], ids); // Deleted control record must never appear.
        foreach (var record in expected)
        {
            var row = page.Locator($"#reportsTable tr[data-warning-id='{record["WarningId"]}']");
            var cells = (await row.Locator("td").AllTextContentsAsync()).Select(t => t.Trim()).ToArray();
            string Text(string column) => Convert.ToString(record[column], CultureInfo.InvariantCulture) ?? "";
            string Date(string column, string format) => record[column] is DateTime date ? date.ToString(format, CultureInfo.InvariantCulture) : "-";
            Assert.Equal(new[] {
                Convert.ToBoolean(record["IsAbsenceDiscussion"]) ? "Discussion" : "Issue", Text("Status"),
                Dash(Text("IssueType")), Dash(Text("Subtype")), Text("Category"), Text("Employee"),
                Dash(Text("Department")), Dash(Text("Manager")), Dash(Text("Creator")), Date("CreatedOn", "yyyy/MM/dd"),
                Date("LegalExpiryDate", "yyyy/MM/dd"), Dash(Text("LatestAction")), Date("LatestActionDate", "yyyy/MM/dd HH:mm")
            }, cells);
        }
        await CountAsync(page, "reportVisibleDetailCount", expected.Count);
        await CountAsync(page, "reportDepartmentCount", expected.Where(r => (string)r["Department"]! != "").Select(r => (string)r["Department"]!).Distinct().Count());
        await CountAsync(page, "reportIssueCount", expected.Count(r => !Convert.ToBoolean(r["IsAbsenceDiscussion"])));
        await CountAsync(page, "reportAbsenceDiscussionCount", expected.Count(r => Convert.ToBoolean(r["IsAbsenceDiscussion"])));
        await CountAsync(page, "reportDraftCount", expected.Count(r => (string)r["Status"]! == "Draft"));
        await CountAsync(page, "reportInProgressCount", expected.Count(r => (string)r["Status"]! is "New" or "In Progress"));
        await CountAsync(page, "reportCompletedCount", expected.Count(r => (string)r["Status"]! == "Completed"));
        await CountAsync(page, "reportDiscussionCount", expected.Count(r => (string)r["IssueType"]! == "Discussion"));
        await CountAsync(page, "reportWarningTypeCount", expected.Count(r => (string)r["IssueType"]! == "Warning" || (string)r["Status"]! == "Warning"));
        if (expected.Count == 0) await Expect(page.Locator("#reportNoResults")).ToBeVisibleAsync();
        else await Expect(page.Locator("#reportNoResults")).ToBeHiddenAsync();
        var breakdown = page.Locator("#reportDepartmentBreakdown tr");
        if (expected.Count > 0)
        {
            var groups = expected.GroupBy(r => string.IsNullOrWhiteSpace((string)r["Department"]!) ? "Unknown" : (string)r["Department"]!).OrderBy(g => g.Key).ToArray();
            await Expect(breakdown).ToHaveCountAsync(groups.Length);
            foreach (var group in groups)
            {
                var row = breakdown.Filter(new() { Has = page.Locator("td:first-child", new() { HasTextString = group.Key }) });
                var values = (await row.Locator("td").AllTextContentsAsync()).Select(t => t.Trim()).ToArray();
                Assert.Equal(new[] { group.Key, group.Count().ToString(), group.Count(r => (string)r["Status"]! == "Draft").ToString(),
                    group.Count(r => (string)r["Status"]! == "In Progress").ToString(), group.Count(r => (string)r["Status"]! == "Completed").ToString(),
                    group.Count(r => (string)r["IssueType"]! == "Warning" || (string)r["Status"]! == "Warning").ToString() }, values);
            }
        }
    }

    private static string Dash(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
    private static Task CountAsync(IPage page, string id, int count) =>
        Expect(page.Locator($"#{id}")).ToHaveTextAsync(count.ToString(CultureInfo.InvariantCulture));
}
