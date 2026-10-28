using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.Admin;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "Admin")]
public sealed class CategoryTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Fact]
    public Task Newly_created_category_appears_on_the_page() => RunAsync(nameof(Newly_created_category_appears_on_the_page), async page =>
    {
        var admin = new AdminPage(page);
        var name = $"UI new category {Guid.NewGuid():N}";

        var response = await admin.CreateCategoryAsync(name);
        Assert.Equal(302, response.Status);
        await Expect(admin.CreateCategoryDialog).ToBeHiddenAsync();
        await admin.OpenCategoriesAsync();

        await Expect(admin.CategoryRow(name)).ToBeVisibleAsync();
    }, "Admin");

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public Task New_category_automatically_includes_default_question(int questionId) =>
        RunAsync($"{nameof(New_category_automatically_includes_default_question)}-{questionId}", async page =>
        {
            var admin = new AdminPage(page);
            var name = $"UI default questions {Guid.NewGuid():N}";

            var response = await admin.CreateCategoryAsync(name);
            Assert.Equal(302, response.Status);

            var links = await Database.QueryAsync("""
                SELECT cq.QuestionId FROM ws.CategoryQuestion cq
                JOIN ws.TransgressionCategory c ON c.CategoryId=cq.CategoryId
                WHERE c.Name=@Name AND cq.QuestionId=@Question AND cq.IsActive=1
                """, ("@Name", name), ("@Question", questionId));
            Assert.Single(links);
            var categoryId = Convert.ToInt32(Assert.Single(await Database.QueryAsync(
                "SELECT CategoryId FROM ws.TransgressionCategory WHERE Name=@Name", ("@Name", name)))["CategoryId"]);
            var questionText = (string)Assert.Single(await Database.QueryAsync(
                "SELECT QuestionText FROM ws.Question WHERE QuestionId=@Id", ("@Id", questionId)))["QuestionText"]!;
            await Expect(admin.CreateCategoryDialog).ToBeHiddenAsync();
            await admin.OpenCategoryQuestionsAsync(categoryId);
            await Expect(admin.LinkedQuestion(questionText)).ToBeVisibleAsync();
        }, "Admin");

    [Fact]
    public Task Editing_category_name_is_saved_and_visible_after_reload() => RunAsync(nameof(Editing_category_name_is_saved_and_visible_after_reload), async page =>
    {
        var admin = new AdminPage(page);
        var oldName = $"UI category before edit {Guid.NewGuid():N}";
        var newName = $"UI renamed category {Guid.NewGuid():N}";
        await admin.CreateCategoryAsync(oldName);
        await Expect(admin.CreateCategoryDialog).ToBeHiddenAsync();
        var id = Convert.ToInt32(Assert.Single(await Database.QueryAsync("SELECT CategoryId FROM ws.TransgressionCategory WHERE Name=@Name", ("@Name", oldName)))["CategoryId"]);

        await admin.EditCategoryAsync(id, newName, active: true);
        await Expect(admin.EditCategoryDialog).ToBeHiddenAsync();
        await admin.OpenAsync();
        await admin.OpenCategoriesAsync();

        await Expect(admin.CategoryRow(newName)).ToBeVisibleAsync();
        Assert.Equal(newName, Assert.Single(await Database.QueryAsync("SELECT Name FROM ws.TransgressionCategory WHERE CategoryId=@Id", ("@Id", id)))["Name"]);
    }, "Admin");

    [Fact]
    public Task Deactivating_category_is_saved() => RunAsync(nameof(Deactivating_category_is_saved), async page =>
    {
        var admin = new AdminPage(page);
        var name = $"UI inactive category {Guid.NewGuid():N}";
        await admin.CreateCategoryAsync(name);
        await Expect(admin.CreateCategoryDialog).ToBeHiddenAsync();
        var id = Convert.ToInt32(Assert.Single(await Database.QueryAsync("SELECT CategoryId FROM ws.TransgressionCategory WHERE Name=@Name", ("@Name", name)))["CategoryId"]);

        await admin.EditCategoryAsync(id, name, active: false);

        Assert.False(Convert.ToBoolean(Assert.Single(await Database.QueryAsync("SELECT IsActive FROM ws.TransgressionCategory WHERE CategoryId=@Id", ("@Id", id)))["IsActive"]));
    }, "Admin");
}
