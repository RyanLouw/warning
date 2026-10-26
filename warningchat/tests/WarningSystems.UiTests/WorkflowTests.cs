using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace WarningSystems.UiTests;

public class WorkflowTests(BrowserFixture fixture) : IClassFixture<BrowserFixture>
{
    [Fact]
    public Task Creating_discussion_persists_visible_employee_dates_description_and_final_status() =>
        fixture.RunAsync("create-discussion", async page =>
        {
            var description = $"UI discussion {Guid.NewGuid():N}";
            await page.GotoAsync("/");
            await page.Locator("#btnAbsenceDiscussion").ClickAsync();
            await page.Locator("#absenceEmployeeId").SelectOptionAsync("alice");
            await page.Locator("#absenceSubType").SelectOptionAsync("2");
            // Replace the default date with a stable past date, then add it twice.
            await page.Locator("#selectedAbsenceDates button").ClickAsync();
            await page.Locator("#absenceDates").FillAsync("2026-01-05");
            await page.Locator("#addAbsenceDate").ClickAsync();
            await page.Locator("#addAbsenceDate").ClickAsync();
            await Expect(page.Locator("#selectedAbsenceDates button")).ToHaveCountAsync(1);
            await page.Locator("#absenceDescription").FillAsync(description);
            await page.RunAndWaitForNavigationAsync(() => page.Locator("#saveAbsenceDiscussion").ClickAsync());
            await using var db = fixture.App.OpenDatabase();
            var answer = await db.WarningAnswers.AsNoTracking().SingleAsync(a => a.QuestionId == 2 && a.AnswerText == description);
            var warning = await db.Warnings.AsNoTracking().SingleAsync(w => w.WarningId == answer.WarningId);
            Assert.Equal("alice", warning.EmployeeId);
            Assert.Equal(3, warning.IssueTypeId);
            Assert.Equal(2, warning.IssueSubTypeId);
            Assert.Equal(6, warning.IssueStatusId);
            Assert.Equal("Validated", warning.Status);
            Assert.Null(warning.LegalExpiryDate);
            var dates = await db.WarningAnswers.AsNoTracking().SingleAsync(a => a.WarningId == warning.WarningId && a.QuestionId == 1);
            using var json = JsonDocument.Parse(dates.AnswerJson!);
            Assert.Equal(new[] { "2026-01-05" }, json.RootElement.GetProperty("dates").EnumerateArray().Select(d => d.GetString()));
            Assert.Equal(new[] { 31 }, await db.WarningCategories.Where(c => c.WarningId == warning.WarningId).Select(c => c.CategoryId).ToArrayAsync());
            await page.Locator("#reports-tab").ClickAsync();
            await page.Locator("[data-bs-target='#reportIssueDetailsCollapse']").ClickAsync();
            var row = page.Locator($"tr[data-warning-id='{warning.WarningId}']");
            await Expect(row).ToBeVisibleAsync();
            Assert.Equal("Discussion", (await row.Locator("td").Nth(0).InnerTextAsync()).Trim());
            Assert.Equal("Validated", (await row.Locator("td").Nth(1).InnerTextAsync()).Trim());
            await Expect(row.Locator("td").Nth(5)).ToHaveTextAsync("Alice Adams");
        }, "User");

    [Fact]
    public Task Invalid_discussion_does_not_write_to_database() => fixture.RunAsync("invalid-discussion", async page =>
    {
        await using var before = fixture.App.OpenDatabase();
        var count = await before.Warnings.CountAsync();
        await page.GotoAsync("/");
        await page.Locator("#btnAbsenceDiscussion").ClickAsync();
        await page.Locator("#saveAbsenceDiscussion").ClickAsync();
        await Expect(page.Locator("#absenceDiscussionModal")).ToBeVisibleAsync();
        Assert.False(await page.Locator("#absenceDiscussionForm").EvaluateAsync<bool>("form => form.checkValidity()"));
        await using var after = fixture.App.OpenDatabase();
        Assert.Equal(count, await after.Warnings.CountAsync());
    }, "User");

    [Fact]
    public Task Admin_creates_question_and_visible_row_matches_database() => fixture.RunAsync("create-question", async page =>
    {
        var question = $"UI question {Guid.NewGuid():N}";
        await page.GotoAsync("/Admin/Index");
        await page.Locator("[data-bs-target='#modalCreateQuestion']").ClickAsync();
        var modal = page.Locator("#modalCreateQuestion");
        await modal.Locator("[name=QuestionText]").FillAsync(question);
        await modal.Locator("[name=ControlType]").SelectOptionAsync("Text");
        await page.RunAndWaitForNavigationAsync(() => modal.GetByRole(AriaRole.Button, new() { Name = "Save Question", Exact = true }).ClickAsync());
        await using var db = fixture.App.OpenDatabase();
        var saved = await db.Questions.AsNoTracking().SingleAsync(q => q.QuestionText == question);
        Assert.Equal("Text", saved.ControlType);
        Assert.True(saved.IsActive);
        Assert.Equal(WarningSystems.Testing.SeedData.ManagerId, saved.CreatedBy);
        await Expect(page.Locator("#questions tr").Filter(new() { HasTextString = question })).ToHaveCountAsync(1);
    }, "Admin");

    [Theory]
    [InlineData("User")]
    [InlineData("Legal")]
    public Task Non_admin_cannot_open_admin_screen(string role) => fixture.RunAsync($"deny-admin-{role}", async page =>
    {
        var response = await page.GotoAsync("/Admin/Index");
        Assert.Equal(403, response!.Status);
    }, role);

    [Fact]
    public Task Task_search_shows_only_matching_database_records() => fixture.RunAsync("task-search", async page =>
    {
        await page.GotoAsync("/");
        await page.Locator("#taskListSearch").FillAsync("Alice");
        await using var db = fixture.App.OpenDatabase();
        var expected = await db.Warnings.AsNoTracking().Where(w => !w.IsDeleted && w.EmployeeId == "alice").CountAsync();
        var rows = page.Locator("#taskListTable .task-list-row:visible");
        await Expect(rows).ToHaveCountAsync(expected);
        foreach (var row in await rows.AllAsync()) await Expect(row.Locator("td").Nth(1)).ToHaveTextAsync("Alice Adams");
        await page.Locator("#taskListSearch").FillAsync("no-such-employee-98765");
        await Expect(rows).ToHaveCountAsync(0);
        await Expect(page.Locator("#taskListNoResults")).ToBeVisibleAsync();
    });
}
