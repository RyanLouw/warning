using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using WarningSystemTest.Pages;
using WarningSystemTest.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.Legal;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "Legal")]
public sealed class WorkflowTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Fact]
    public Task Opening_new_issue_moves_it_to_legal_review_in_progress() => RunAsync("legal-open", async page =>
    {
        var id = Database.Warnings["New"];
        Assert.Equal(200, (await page.GotoAsync($"/Legal/Index/{id}"))!.Status);
        await Expect(page.Locator("body")).ToContainTextAsync("UI Alice");
        var row = Assert.Single(await Database.QueryAsync("SELECT w.LastStatusChangedBy,s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId WHERE WarningId=@Id", ("@Id", id)));
        Assert.Equal(3, Convert.ToInt32(row["IssueStatusGroup"]));
        Assert.Equal("test-legal", row["LastStatusChangedBy"]);
    }, "Legal");

    [Fact]
    public Task Interaction_note_is_persisted_and_visible() => RunAsync("legal-note", async page =>
    {
        var id = Database.Warnings["Decision"];
        var note = $"UI legal interaction {Guid.NewGuid():N}";
        await new LegalPage(page).OpenAsync(id);
        await page.Locator("[data-bs-target='#addNoteModal']").ClickAsync();
        await page.Locator("#addInteractionNote").FillAsync(note);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnAddInteraction").ClickAsync(), r => r.Url.Contains("/Legal/SaveEvidenceAttachment") && r.Request.Method=="POST");
        Assert.True(response.Ok);
        var row = Assert.Single(await Database.QueryAsync("SELECT CreatedBy,EvidenceId FROM ws.WarningNote WHERE WarningId=@Id AND NoteText=@Text", ("@Id", id), ("@Text", note)));
        Assert.Equal("test-legal", row["CreatedBy"]);
        Assert.Null(row["EvidenceId"]);
        await new LegalPage(page).OpenAsync(id);
        await Expect(page.Locator("#interactionsTable")).ToContainTextAsync(note);
    }, "Legal");

    [Fact]
    public Task Supporting_document_and_note_are_linked_in_database() => RunAsync("legal-document", async page =>
    {
        var id = Database.Warnings["Decision"];
        const string note = "UI document supporting legal review";
        var bytes = Encoding.UTF8.GetBytes("UI legal evidence file");
        await new LegalPage(page).OpenAsync(id);
        await page.Locator("#docs-tab").ClickAsync();
        await page.Locator("[data-bs-target='#uploadDocsModal']").ClickAsync();
        await page.Locator("#uploadFiles").SetInputFilesAsync(new FilePayload { Name="legal-proof.txt", MimeType="text/plain", Buffer=bytes });
        await page.Locator("#uploadNotes").FillAsync(note);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnUploadDocs").ClickAsync(), r => r.Url.Contains("/Legal/SaveEvidenceAttachment") && r.Request.Method=="POST");
        Assert.True(response.Ok);
        var file = Assert.Single(await Database.QueryAsync("SELECT * FROM ws.WarningEvidence WHERE WarningId=@Id", ("@Id", id)));
        Assert.Equal(bytes.Length, Convert.ToInt32(file["FileSizeBytes"]));
        Assert.Equal("test-legal", file["UploadedBy"]);
        Assert.Equal(file["EvidenceId"], Assert.Single(await Database.QueryAsync("SELECT EvidenceId FROM ws.WarningNote WHERE WarningId=@Id AND NoteText=@Text", ("@Id", id), ("@Text", note)))["EvidenceId"]);
        await new LegalPage(page).OpenAsync(id);
        await page.Locator("#docs-tab").ClickAsync();
        await Expect(page.Locator("#docs")).ToContainTextAsync("legal-proof.txt");
    }, "Legal");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Legal_decision_persists_subtype_and_notifies_creator(bool attachment) => RunAsync($"legal-decision-{attachment}", async page =>
    {
        var id = Database.Warnings["Decision"];
        var subtype = Convert.ToInt32(Assert.Single(await Database.QueryAsync("SELECT MIN(IssueSubTypeId) AS Id FROM ws.LookupIssueSubTypes WHERE IssueTypeId=2 AND IsActive=1"))["Id"]);
        await new LegalPage(page).OpenAsync(id);
        await page.Locator("[data-bs-target='#completeModal']").ClickAsync();
        await page.Locator("#issueType-2").CheckAsync();
        await page.Locator("#issueSubtype").SelectOptionAsync(subtype.ToString());
        if (attachment) await page.Locator("#completeFile").SetInputFilesAsync(new FilePayload { Name="decision.txt", MimeType="text/plain", Buffer=Encoding.UTF8.GetBytes("UI legal decision") });
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnComplete").ClickAsync(), r => r.Url.Contains("/Legal/CompleteTransgression") && r.Request.Method=="POST");
        Assert.True(response.Ok);
        var row = Assert.Single(await Database.QueryAsync("SELECT w.IssueTypeId,w.IssueSubTypeId,w.LastStatusChangedBy,s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId WHERE WarningId=@Id", ("@Id", id)));
        Assert.Equal(2, Convert.ToInt32(row["IssueTypeId"]));
        Assert.Equal(subtype, Convert.ToInt32(row["IssueSubTypeId"]));
        Assert.Equal(4, Convert.ToInt32(row["IssueStatusGroup"]));
        Assert.Equal("test-legal", row["LastStatusChangedBy"]);
        using var messages = JsonDocument.Parse(await (await page.Context.APIRequest.GetAsync("/__testing/outbox")).TextAsync());
        var mail = Assert.Single(messages.RootElement.EnumerateArray().Where(m => m.GetProperty("subject").GetString()==$"Warning System | Issue {id} updated"));
        Assert.Contains("user@example.test", mail.GetProperty("recipients").EnumerateArray().Select(r => r.GetString()));
        Assert.Equal(attachment ? 1 : 0, mail.GetProperty("attachments").GetArrayLength());
        Assert.Equal(attachment ? 1 : 0, (await Database.QueryAsync("SELECT EvidenceId FROM ws.WarningEvidence WHERE WarningId=@Id", ("@Id", id))).Count);
    }, "Legal");

    [Fact]
    public Task Issued_warning_can_be_validated() => RunAsync("legal-validate", async page =>
    {
        var id = Database.Warnings["Issued"];
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await new LegalPage(page).OpenAsync(id);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnValidateIssue").ClickAsync(), r => r.Url.Contains("/Legal/ValidateTransgression") && r.Request.Method=="POST");
        Assert.True(response.Ok);
        Assert.Equal(6, Convert.ToInt32(Assert.Single(await Database.QueryAsync("SELECT s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId WHERE WarningId=@Id", ("@Id", id)))["IssueStatusGroup"]));
        await new LegalPage(page).OpenAsync(id);
        await Expect(page.Locator("#btnValidateIssue")).ToHaveCountAsync(0);
    }, "Legal");

    [Fact]
    public Task Due_date_change_is_persisted_with_reason() => RunAsync("legal-due-date", async page =>
    {
        var id = Database.Warnings["Decision"];
        var date = DateTime.Today.AddDays(14);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) date=date.AddDays(1);
        const string reason = "UI scheduled extension for investigation";
        await new LegalPage(page).OpenAsync(id);
        await page.Locator("[data-bs-target='#dueDateModal']").ClickAsync();
        await page.Locator("#dueDateValue").FillAsync(date.ToString("yyyy-MM-dd"));
        await page.Locator("#dueDateReason").FillAsync(reason);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnSaveDueDate").ClickAsync(), r => r.Url.Contains("/Legal/UpdateDueDate") && r.Request.Method=="POST");
        Assert.True(response.Ok);
        Assert.Equal(date.Date, ((DateTime)Assert.Single(await Database.QueryAsync("SELECT LegalExpiryDate FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["LegalExpiryDate"]!).Date);
        Assert.NotEmpty(await Database.QueryAsync("SELECT NoteId FROM ws.WarningNote WHERE WarningId=@Id AND CHARINDEX(@Reason,NoteText)>0", ("@Id", id), ("@Reason", reason)));
    }, "Legal");

    [Fact]
    public Task Weekend_due_date_is_rejected_without_a_change() => RunAsync("legal-weekend", async page =>
    {
        var id = Database.Warnings["Decision"];
        var before=Assert.Single(await Database.QueryAsync("SELECT LegalExpiryDate FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["LegalExpiryDate"];
        var date=DateTime.Today.AddDays(14);
        while (date.DayOfWeek!=DayOfWeek.Saturday) date=date.AddDays(1);
        await new LegalPage(page).OpenAsync(id);
        await page.Locator("[data-bs-target='#dueDateModal']").ClickAsync();
        await page.Locator("#dueDateValue").FillAsync(date.ToString("yyyy-MM-dd"));
        await page.Locator("#dueDateReason").FillAsync("UI invalid weekend date");
        await page.Locator("#btnSaveDueDate").ClickAsync();
        await Expect(page.Locator("#dueDateSaveMsg")).ToContainTextAsync("weekend");
        Assert.Equal(before, Assert.Single(await Database.QueryAsync("SELECT LegalExpiryDate FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["LegalExpiryDate"]);
    }, "Legal");

    [Fact]
    public Task Legal_category_change_updates_links_and_audit_note() => RunAsync("legal-categories", async page =>
    {
        var id = Database.Warnings["Decision"];
        await new LegalPage(page).OpenAsync(id);
        await page.Locator("[data-bs-target='#editIssueDetailsModal']").ClickAsync();
        foreach (var box in await page.Locator(".legal-category-checkbox:checked").AllAsync()) await box.UncheckAsync();
        await page.Locator($".legal-category-checkbox[value='{Database.AttendanceCategory}']").CheckAsync();
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#editIssueDetailsModal").GetByRole(AriaRole.Button, new() { Name="Save Changes", Exact=true }).ClickAsync(), r => r.Url.Contains("/Legal/UpdateIssueDetails") && r.Request.Method=="POST");
        Assert.True(response.Ok);
        Assert.Equal(Database.AttendanceCategory, Convert.ToInt32(Assert.Single(await Database.QueryAsync("SELECT CategoryId FROM ws.WarningCategory WHERE WarningId=@Id", ("@Id", id)))["CategoryId"]));
        Assert.NotEmpty(await Database.QueryAsync("SELECT NoteId FROM ws.WarningNote WHERE WarningId=@Id AND NoteText LIKE 'Legal changed categories%'", ("@Id", id)));
    }, "Legal");

    [Fact]
    public Task Team_lead_visibility_flag_is_persisted() => RunAsync("legal-visibility", async page =>
    {
        var id=Database.Warnings["Decision"];
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await new LegalPage(page).OpenAsync(id);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnTeamLeadVisibility").ClickAsync(), r => r.Url.Contains("/Legal/SetTeamLeadVisibility") && r.Request.Method=="POST");
        Assert.True(response.Ok);
        Assert.True(Convert.ToBoolean(Assert.Single(await Database.QueryAsync("SELECT HideFromTeamLead FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["HideFromTeamLead"]));
    }, "Legal");

}
