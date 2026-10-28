using Microsoft.Playwright;

namespace WarningSystemTest.Pages;

public sealed class AdminPage(IPage page) : WarningPage(page)
{
    public ILocator CreateQuestionDialog => Page.Locator("#modalCreateQuestion");
    public ILocator CreateCategoryDialog => Page.Locator("#modalCreateCategory");
    public ILocator EditCategoryDialog => Page.Locator("#modalEditCategory");
    public Task<int> OpenAsync() => OpenRouteAsync("/Admin/Index");
    public Task OpenQuestionsAsync() => Page.Locator("[data-bs-target='#questions']").ClickAsync();
    public Task OpenCategoriesAsync() => Page.Locator("[data-bs-target='#categories']").ClickAsync();
    public Task OpenNewQuestionAsync() => Page.Locator("[data-bs-target='#modalCreateQuestion']").ClickAsync();
    public async Task<IResponse> CreateQuestionAsync(string name, string control = "Text", Func<ILocator,Task>? configure = null)
    {
        await OpenAsync();
        await OpenNewQuestionAsync();
        await CreateQuestionDialog.Locator("[name=QuestionText]").FillAsync(name);
        await CreateQuestionDialog.Locator("[name=ControlType]").SelectOptionAsync(control);
        if (configure is not null) await configure(CreateQuestionDialog);
        return await PostByClickAsync(CreateQuestionDialog.GetByRole(AriaRole.Button, new() { Name="Save Question", Exact=true }), "/Admin/CreateQuestion");
    }
    public async Task<IResponse> CreateCategoryAsync(string name)
    {
        await OpenAsync();
        await OpenCategoriesAsync();
        await Page.Locator("[data-bs-target='#modalCreateCategory']").ClickAsync();
        await CreateCategoryDialog.Locator("[name=Name]").FillAsync(name);
        return await PostByClickAsync(CreateCategoryDialog.Locator("button[type=submit]"), "/Admin/CreateCategory");
    }
    public async Task EditCategoryAsync(int id, string name, bool active)
    {
        await OpenAsync();
        await OpenCategoriesAsync();
        await Page.Locator($"[data-bs-target='#modalEditCategory'][data-id='{id}']").ClickAsync();
        await Page.Locator("#editCategoryName").FillAsync(name);
        await Page.Locator("#editCategoryActive").SetCheckedAsync(active);
        await PostByClickAsync(EditCategoryDialog.Locator("button[type=submit]"), "/Admin/EditCategory");
    }
    public async Task OpenCategoryQuestionsAsync(int id)
    {
        await OpenCategoriesAsync();
        await Page.Locator($"[data-bs-target='#modalManageCategoryQuestions'][data-id='{id}']").ClickAsync();
    }
    public ILocator LinkedQuestion(string text) => Page.Locator("#linkedQuestionsTable tr")
        .Filter(new() { HasTextString=text });
    public ILocator CategoryRow(string name) => Page.Locator("#categories tr").Filter(new() { HasTextString=name });
    public ILocator QuestionRow(string name) => Page.Locator("#questions tr").Filter(new() { HasTextString=name });
}
