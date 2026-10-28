using Microsoft.Playwright;

namespace WarningSystemTest.Pages;

public sealed class TeamLeadPage(IPage page) : WarningPage(page)
{
    public ILocator AddEvidence => Page.GetByRole(AriaRole.Button, new() { Name = "Add Evidence", Exact = true });
    public ILocator NotifyLegal => Page.Locator("#btnNotifyLegalComplete");
    public ILocator EvidenceDialog => Page.Locator("#uploadDocsModal");
    public Task<int> OpenAsync(long issueId) => OpenRouteAsync($"/Transgression/TeamLeadWarning/{issueId}");
    public Task OpenEvidenceDialogAsync() => AddEvidence.ClickAsync();
    public Task SetNoteAsync(string note) => EvidenceDialog.Locator("[name=NoteText]").FillAsync(note);
    public Task SetFilesAsync(FilePayload file) => EvidenceDialog.Locator("[name=Files]").SetInputFilesAsync(file);
    public Task CancelEvidenceAsync() => EvidenceDialog.GetByRole(AriaRole.Button, new() { Name="Cancel", Exact=true }).ClickAsync();
    public Task<IResponse> SaveEvidenceAsync() => PostByClickAsync(
        EvidenceDialog.Locator("button[type=submit]"), "/Transgression/AddTeamLeadEvidence");
    public Task<IResponse> NotifyLegalAsync() => PostByClickAsync(NotifyLegal, "/Transgression/NotifyLegalIssueCompleted");
}
