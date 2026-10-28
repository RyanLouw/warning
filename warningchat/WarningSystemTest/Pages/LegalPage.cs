using Microsoft.Playwright;

namespace WarningSystemTest.Pages;

public sealed class LegalPage(IPage page) : WarningPage(page)
{
    public ILocator Action(string name) => name switch
    {
        "Validate" => Page.Locator("#btnValidateIssue"),
        "Visibility" => Page.Locator("#btnTeamLeadVisibility"),
        "MoreInformation" => Page.Locator("[data-bs-target='#requestTeamLeadInfoModal']"),
        "Decision" => Page.Locator("[data-bs-target='#completeModal']"),
        "CategoriesAndDates" => Page.Locator("[data-bs-target='#editIssueDetailsModal']"),
        "DueDate" => Page.Locator("[data-bs-target='#dueDateModal']"),
        _ => throw new ArgumentException($"Unknown Legal action: {name}", nameof(name))
    };
    public ILocator DueDateDialog => Page.Locator("#dueDateModal");
    public ILocator DueDateMessage => Page.Locator("#dueDateSaveMsg");
    public ILocator InformationDialog => Page.Locator("#requestTeamLeadInfoModal");
    public Task<int> OpenAsync(long issueId) => OpenRouteAsync($"/Legal/Index/{issueId}");
    public Task OpenDueDateAsync() => Action("DueDate").ClickAsync();
    public async Task FillDueDateAsync(string date, string reason)
    {
        await Page.Locator("#dueDateValue").FillAsync(date);
        await Page.Locator("#dueDateReason").FillAsync(reason);
    }
    public Task<IResponse> SaveDueDateAsync() => PostByClickAsync(Page.Locator("#btnSaveDueDate"), "/Legal/UpdateDueDate");
    public Task ClickSaveDueDateAsync() => Page.Locator("#btnSaveDueDate").ClickAsync();
    public Task CancelDueDateAsync() => DueDateDialog.GetByRole(AriaRole.Button, new() { Name="Cancel", Exact=true }).ClickAsync();
    public Task OpenInformationRequestAsync() => Action("MoreInformation").ClickAsync();
    public Task SetInformationMessageAsync(string message) => Page.Locator("#requestInfoMessage").FillAsync(message);
    public Task<IResponse> SendInformationRequestAsync() => PostByClickAsync(Page.Locator("#btnRequestTeamLeadInfo"), "/Legal/RequestTeamLeadInformation");
    public Task CancelInformationRequestAsync() => InformationDialog.GetByRole(AriaRole.Button, new() { Name="Cancel", Exact=true }).ClickAsync();
    public Task<IResponse> ValidateAsync() => PostByClickAsync(Action("Validate"), "/Legal/ValidateTransgression");
    public Task<IResponse> ToggleVisibilityAsync() => PostByClickAsync(Action("Visibility"), "/Legal/SetTeamLeadVisibility");
}
