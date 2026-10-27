using System.Text.Json;
using Microsoft.Playwright;
using WarningSystemTest.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI;

[Collection("Warning browser")]
[Trait("Category", "UI")]
public sealed class DiscussionTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Fact]
    public Task Discussion_is_visible_and_database_contains_employee_date_description_and_final_status() => RunAsync("discussion", async page =>
    {
        var description = $"UI discussion {Guid.NewGuid():N}";
        await page.GotoAsync("/");
        await page.Locator("#btnAbsenceDiscussion").ClickAsync();
        await page.Locator("#absenceEmployeeId").SelectOptionAsync("test-employee-alice");
        await page.Locator("#absenceSubType").SelectOptionAsync(Database.DiscussionSubtype.ToString());
        await page.Locator("#absenceDates").FillAsync("2026-01-05");
        await page.Locator("#absenceDescription").FillAsync(description);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#saveAbsenceDiscussion").ClickAsync(), r => r.Url.Contains("/Home/CreateAbsenceDiscussion") && r.Request.Method=="POST");
        Assert.True(response.Ok);
        await Expect(page.Locator("#absenceDiscussionModal")).ToBeHiddenAsync();
        var row = Assert.Single(await Database.QueryAsync("SELECT w.*,s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId JOIN ws.WarningAnswer a ON a.WarningId=w.WarningId WHERE a.QuestionId=2 AND a.AnswerText=@Text", ("@Text", description)));
        Assert.Equal("test-employee-alice", row["EmployeeId"]);
        Assert.Equal(3, Convert.ToInt32(row["IssueTypeId"]));
        Assert.Equal(6, Convert.ToInt32(row["IssueStatusGroup"]));
        Assert.Null(row["LegalExpiryDate"]);
        using var dates = JsonDocument.Parse((string)Assert.Single(await Database.QueryAsync("SELECT AnswerJson FROM ws.WarningAnswer WHERE WarningId=@Id AND QuestionId=1", ("@Id", row["WarningId"])))["AnswerJson"]!);
        Assert.Equal(new[] { "2026-01-05" }, dates.RootElement.GetProperty("dates").EnumerateArray().Select(d => d.GetString()));
        await page.Locator("#reports-tab").ClickAsync();
        await page.Locator("[data-bs-target='#reportIssueDetailsCollapse']").ClickAsync();
        var visible = page.Locator($"tr[data-warning-id='{row["WarningId"]}']");
        await Expect(visible).ToBeVisibleAsync();
        await Expect(visible.Locator("td").Nth(0)).ToHaveTextAsync("Discussion");
        await Expect(visible.Locator("td").Nth(5)).ToHaveTextAsync("UI Alice");
    });

    [Fact]
    public Task Missing_discussion_fields_prevent_a_database_write() => RunAsync("discussion-invalid", async page =>
    {
        var before = Assert.Single(await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning"))["Total"];
        await page.GotoAsync("/");
        await page.Locator("#btnAbsenceDiscussion").ClickAsync();
        await page.Locator("#saveAbsenceDiscussion").ClickAsync();
        Assert.False(await page.Locator("#absenceDiscussionForm").EvaluateAsync<bool>("form => form.checkValidity()"));
        Assert.Equal(before, Assert.Single(await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning"))["Total"]);
    });
}
