using System.Text.Json;
using Microsoft.Playwright;
using WarningSystemTest.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI;

[Collection("Warning browser")]
[Trait("Category", "UI")]
public sealed class AdminTests(BrowserFixture browser) : UiTestBase(browser)
{
    private async Task<(string Name, int Id)> CreateQuestionAsync(IPage page, string control = "Text", Func<ILocator, Task>? configure = null, string? name = null)
    {
        name ??= $"UI question {Guid.NewGuid():N}";
        await page.GotoAsync("/Admin/Index");
        await page.Locator("[data-bs-target='#modalCreateQuestion']").ClickAsync();
        var modal = page.Locator("#modalCreateQuestion");
        await modal.Locator("[name=QuestionText]").FillAsync(name);
        await modal.Locator("[name=ControlType]").SelectOptionAsync(control);
        if (configure is not null) await configure(modal);
        var response = await page.RunAndWaitForResponseAsync(() => modal.GetByRole(AriaRole.Button, new() { Name = "Save Question", Exact = true }).ClickAsync(), r => r.Url.Contains("/Admin/CreateQuestion") && r.Request.Method == "POST");
        Assert.Equal(302, response.Status);
        await Expect(modal).ToBeHiddenAsync();
        var row = Assert.Single(await Database.QueryAsync("SELECT * FROM ws.Question WHERE QuestionText=@Name AND ControlType=@Control", ("@Name", name), ("@Control", control)));
        Assert.Equal("test-admin", row["CreatedBy"]);
        await Expect(page.Locator("#questions tr").Filter(new() { HasTextString = name })).ToHaveCountAsync(1);
        return (name, Convert.ToInt32(row["QuestionId"]));
    }

    [Fact]
    public Task Text_question_persists_and_is_visible() => RunAsync("admin-text", async page => { await CreateQuestionAsync(page); }, "Admin");

    [Fact]
    public Task Dropdown_question_preserves_options_and_default() => RunAsync("admin-dropdown", async page =>
    {
        var (_, id) = await CreateQuestionAsync(page, "Dropdown", async modal =>
        {
            foreach (var option in new[] { "Yes", "No", "Yes" })
            {
                await modal.Locator("#optionInput").FillAsync(option);
                await modal.Locator("#btnAddOption").ClickAsync();
            }
            await Expect(modal.Locator("#optionsList li")).ToHaveCountAsync(2);
            await modal.Locator("#defaultOptionSelect").SelectOptionAsync("No");
        });
        using var config = JsonDocument.Parse((string)Assert.Single(await Database.QueryAsync("SELECT DefaultConfigJson FROM ws.Question WHERE QuestionId=@Id", ("@Id", id)))["DefaultConfigJson"]!);
        Assert.Equal(new[] { "Yes", "No" }, config.RootElement.GetProperty("options").EnumerateArray().Select(v => v.GetString()));
        Assert.Equal("No", config.RootElement.GetProperty("default").GetString());
    }, "Admin");

    [Fact]
    public Task Numeric_question_preserves_range_and_step() => RunAsync("admin-number", async page =>
    {
        var (_, id) = await CreateQuestionAsync(page, "Number", async modal =>
        {
            await modal.Locator("#numMin").FillAsync("1");
            await modal.Locator("#numMax").FillAsync("10");
            await modal.Locator("#numStep").FillAsync("2");
        });
        using var config = JsonDocument.Parse((string)Assert.Single(await Database.QueryAsync("SELECT DefaultConfigJson FROM ws.Question WHERE QuestionId=@Id", ("@Id", id)))["DefaultConfigJson"]!);
        Assert.Equal(1, config.RootElement.GetProperty("min").GetInt32());
        Assert.Equal(10, config.RootElement.GetProperty("max").GetInt32());
        Assert.Equal(2, config.RootElement.GetProperty("step").GetDouble());
    }, "Admin");

    [Fact]
    public Task Boolean_question_preserves_default() => RunAsync("admin-boolean", async page =>
    {
        var (_, id) = await CreateQuestionAsync(page, "TrueFalse", modal => modal.Locator("#defaultBooleanSelect").SelectOptionAsync("false"));
        using var config = JsonDocument.Parse((string)Assert.Single(await Database.QueryAsync("SELECT DefaultConfigJson FROM ws.Question WHERE QuestionId=@Id", ("@Id", id)))["DefaultConfigJson"]!);
        Assert.Equal("False", config.RootElement.GetProperty("default").GetString());
    }, "Admin");

    [Fact]
    public Task Duplicate_question_is_rejected_without_an_extra_row() => RunAsync("admin-duplicate", async page =>
    {
        var (name, _) = await CreateQuestionAsync(page);
        await CreateQuestionAsync(page, name: name);
        await Expect(page.Locator("body")).ToContainTextAsync("already exists");
        Assert.Single(await Database.QueryAsync("SELECT QuestionId FROM ws.Question WHERE QuestionText=@Name AND ControlType='Text'", ("@Name", name)));
    }, "Admin");

    [Fact]
    public Task Editing_question_records_changes_and_audit() => RunAsync("admin-edit-question", async page =>
    {
        var (_, id) = await CreateQuestionAsync(page);
        var updated = $"UI edited question {Guid.NewGuid():N}";
        await page.Locator($"[data-bs-target='#modalEditQuestion'][data-id='{id}']").ClickAsync();
        await page.Locator("#editQuestionText").FillAsync(updated);
        await page.Locator("#editQuestionActive").UncheckAsync();
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#modalEditQuestion button[type=submit]").ClickAsync(), r => r.Url.Contains("/Admin/EditQuestion") && r.Request.Method == "POST");
        Assert.Equal(302, response.Status);
        await Expect(page.Locator("#modalEditQuestion")).ToBeHiddenAsync();
        var row = Assert.Single(await Database.QueryAsync("SELECT * FROM ws.Question WHERE QuestionId=@Id", ("@Id", id)));
        Assert.Equal(updated, row["QuestionText"]);
        Assert.False(Convert.ToBoolean(row["IsActive"]));
        Assert.Equal("test-admin", row["UpdatedBy"]);
        var audits = await Database.QueryAsync("SELECT NewValues FROM ws.AdminAuditLog WHERE EntityName='Question' AND EntityId=CAST(@Id AS varchar(50))", ("@Id", id));
        Assert.Contains(audits, audit => ((string?)audit["NewValues"])?.Contains(updated) == true);
    }, "Admin");

    [Fact]
    public Task New_category_has_persisted_default_question_links() => RunAsync("admin-category", async page =>
    {
        var name = $"UI category {Guid.NewGuid():N}";
        await page.GotoAsync("/Admin/Index");
        await page.Locator("[data-bs-target='#categories']").ClickAsync();
        await page.Locator("[data-bs-target='#modalCreateCategory']").ClickAsync();
        await page.Locator("#modalCreateCategory [name=Name]").FillAsync(name);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#modalCreateCategory button[type=submit]").ClickAsync(), r => r.Url.Contains("/Admin/CreateCategory") && r.Request.Method == "POST");
        Assert.Equal(302, response.Status);
        await Expect(page.Locator("#modalCreateCategory")).ToBeHiddenAsync();
        var row = Assert.Single(await Database.QueryAsync("SELECT CategoryId,CreatedBy FROM ws.TransgressionCategory WHERE Name=@Name", ("@Name", name)));
        Assert.Equal("test-admin", row["CreatedBy"]);
        Assert.Equal(new[] { 1, 2, 3 }, (await Database.QueryAsync("SELECT QuestionId FROM ws.CategoryQuestion WHERE CategoryId=@Id AND IsActive=1 ORDER BY QuestionId", ("@Id", row["CategoryId"]))).Select(q => Convert.ToInt32(q["QuestionId"])));
        await page.Locator("[data-bs-target='#categories']").ClickAsync();
        await Expect(page.Locator("#categories tr").Filter(new() { HasTextString = name })).ToHaveCountAsync(1);
    }, "Admin");

    [Fact]
    public Task Question_link_is_saved_with_required_flag_and_sort_order() => RunAsync("admin-question-link", async page =>
    {
        var (name, id) = await CreateQuestionAsync(page);
        await page.Locator("[data-bs-target='#categories']").ClickAsync();
        await page.Locator($"[data-bs-target='#modalManageCategoryQuestions'][data-id='{Database.AttendanceCategory}']").ClickAsync();
        await page.Locator("#manageAddQuestionSelect").SelectOptionAsync(id.ToString());
        await page.Locator("#manageIsRequired").CheckAsync();
        await page.Locator("#manageSortOrder").FillAsync("9");
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnAddQuestionToCategory").ClickAsync(), r => r.Url.Contains("/Admin/SetCategoryQuestionLink") && r.Request.Method == "POST");
        Assert.True(response.Ok);
        var row = Assert.Single(await Database.QueryAsync("SELECT IsActive,IsRequired,SortOrder FROM ws.CategoryQuestion WHERE CategoryId=@Category AND QuestionId=@Question", ("@Category", Database.AttendanceCategory), ("@Question", id)));
        Assert.True(Convert.ToBoolean(row["IsActive"]));
        Assert.True(Convert.ToBoolean(row["IsRequired"]));
        Assert.Equal(9, Convert.ToInt32(row["SortOrder"]));
        await Expect(page.Locator("#linkedQuestionsTable tr").Filter(new() { HasTextString = name })).ToHaveCountAsync(1);
    }, "Admin");

    [Theory]
    [InlineData("User")]
    [InlineData("Legal")]
    [InlineData("Chairperson")]
    public Task Non_admin_cannot_access_admin_routes(string role) => RunAsync($"deny-admin-{role}", async page =>
    {
        Assert.Equal(403, (await page.Context.APIRequest.GetAsync("/Admin/Index")).Status);
        Assert.Equal(403, (await page.Context.APIRequest.GetAsync($"/Admin/GetAvailableQuestions?categoryId={Database.AttendanceCategory}")).Status);
    }, role);
}
