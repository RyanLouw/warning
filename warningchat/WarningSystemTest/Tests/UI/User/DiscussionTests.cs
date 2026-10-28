using System.Text.Json;
using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.User;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "User")]
public sealed class DiscussionTests(BrowserFixture browser) : UiTestBase(browser)
{
    private async Task<long> CreateDiscussionAsync(Microsoft.Playwright.IPage page)
    {
        var home = new HomePage(page);
        var description = $"UI discussion {Guid.NewGuid():N}";
        await home.OpenAsync();
        var response = await home.CreateDiscussionAsync("test-employee-alice", Database.DiscussionSubtype, "2026-01-05", description);
        Assert.True(response.Ok, $"Creating a discussion returned HTTP {response.Status}: {await response.TextAsync()}");
        await Expect(home.DiscussionDialog).ToBeHiddenAsync();
        return Convert.ToInt64(Assert.Single(await Database.QueryAsync("SELECT WarningId FROM ws.WarningAnswer WHERE QuestionId=2 AND AnswerText=@Text", ("@Text", description)))["WarningId"]);
    }

    [Fact]
    public Task New_discussion_is_saved_for_the_selected_employee() => RunAsync(nameof(New_discussion_is_saved_for_the_selected_employee), async page =>
    {
        var id = await CreateDiscussionAsync(page);

        Assert.Equal("test-employee-alice", Assert.Single(await Database.QueryAsync("SELECT EmployeeId FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["EmployeeId"]);
    });

    [Fact]
    public Task New_discussion_has_the_selected_date() => RunAsync(nameof(New_discussion_has_the_selected_date), async page =>
    {
        var id = await CreateDiscussionAsync(page);
        var row = Assert.Single(await Database.QueryAsync("SELECT AnswerJson FROM ws.WarningAnswer WHERE WarningId=@Id AND QuestionId=1", ("@Id", id)));
        using var answer = JsonDocument.Parse((string)row["AnswerJson"]!);

        Assert.Equal(new[] { "2026-01-05" }, answer.RootElement.GetProperty("dates").EnumerateArray().Select(date => date.GetString()));
    });

    [Fact]
    public Task New_discussion_is_closed_without_legal_review() => RunAsync(nameof(New_discussion_is_closed_without_legal_review), async page =>
    {
        var id = await CreateDiscussionAsync(page);
        var row = Assert.Single(await Database.QueryAsync("SELECT s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId WHERE w.WarningId=@Id", ("@Id", id)));

        Assert.Equal(6, Convert.ToInt32(row["IssueStatusGroup"]));
    });

    [Fact]
    public Task New_discussion_has_no_legal_due_date() => RunAsync(nameof(New_discussion_has_no_legal_due_date), async page =>
    {
        var id = await CreateDiscussionAsync(page);

        Assert.Null(Assert.Single(await Database.QueryAsync("SELECT LegalExpiryDate FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["LegalExpiryDate"]);
    });

    [Fact]
    public Task New_discussion_appears_in_reports() => RunAsync(nameof(New_discussion_appears_in_reports), async page =>
    {
        await CreateDiscussionAsync(page);
        var home = new HomePage(page);

        await home.OpenReportsAsync();

        await Expect(home.ReportRows.Filter(new() { Has = page.Locator("td:nth-child(6):text-is('UI Alice')") })
            .Filter(new() { Has = page.Locator("td:first-child:text-is('Discussion')") })).ToHaveCountAsync(1);
    });

    [Fact]
    public Task Missing_discussion_fields_prevent_a_database_write() => RunAsync(nameof(Missing_discussion_fields_prevent_a_database_write), async page =>
    {
        var before = Assert.Single(await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning"))["Total"];
        var home = new HomePage(page);
        await home.OpenAsync();
        await home.OpenDiscussionAsync();

        await home.ClickSaveDiscussionAsync();

        Assert.False(await home.DiscussionForm.EvaluateAsync<bool>("form => form.checkValidity()"));
        Assert.Equal(before, Assert.Single(await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning"))["Total"]);
    });
}
