using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WarningSystemTest.Pages;
using WarningSystemTest.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.User;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "User")]
public sealed class WarningWizardTests(BrowserFixture browser) : UiTestBase(browser)
{
    private async Task<long> CreateDraftAsync(IPage page)
    {
        var wizard = new WarningWizardPage(page);
        await wizard.OpenAsync();
        await wizard.SelectEmployeeAsync("test-employee-alice");
        await wizard.SelectCategoryAsync(Database.AttendanceCategory);
        await wizard.SelectCategoryAsync(Database.ConductCategory);
        var response = await wizard.SaveDraftAsync();
        Assert.Equal(302, response.Status);
        await Expect(page).ToHaveURLAsync(new Regex(@"/Transgression/Index/\d+"));
        return await wizard.IssueIdAsync();
    }

    [Fact]
    public Task New_draft_is_saved_for_the_selected_employee() => RunAsync(nameof(New_draft_is_saved_for_the_selected_employee), async page =>
    {
        var id = await CreateDraftAsync(page);

        Assert.Equal("test-employee-alice", Assert.Single(await Database.QueryAsync(
            "SELECT EmployeeId FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["EmployeeId"]);
    });

    [Fact]
    public Task New_draft_saves_every_selected_category_once() => RunAsync(nameof(New_draft_saves_every_selected_category_once), async page =>
    {
        var id = await CreateDraftAsync(page);
        var categories = await Database.QueryAsync("SELECT CategoryId FROM ws.WarningCategory WHERE WarningId=@Id", ("@Id", id));

        Assert.Equal(new[] { Database.AttendanceCategory, Database.ConductCategory }.Order(),
            categories.Select(row => Convert.ToInt32(row["CategoryId"])).Order());
    });

    [Fact]
    public Task New_draft_records_the_logged_in_creator() => RunAsync(nameof(New_draft_records_the_logged_in_creator), async page =>
    {
        var id = await CreateDraftAsync(page);

        Assert.Equal("test-user", Assert.Single(await Database.QueryAsync(
            "SELECT CreatedBy FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["CreatedBy"]);
    });

    [Fact]
    public Task New_issue_starts_in_the_draft_status_group() => RunAsync(nameof(New_issue_starts_in_the_draft_status_group), async page =>
    {
        var id = await CreateDraftAsync(page);
        var row = Assert.Single(await Database.QueryAsync("SELECT s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId WHERE WarningId=@Id", ("@Id", id)));

        Assert.Equal(1, Convert.ToInt32(row["IssueStatusGroup"]));
    });

    [Fact]
    public Task Missing_employee_is_blocked_without_database_write() => RunAsync("draft-validation", async page =>
    {
        var before = (await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning")).Single()["Total"];
        await new WarningWizardPage(page).OpenAsync();
        await page.Locator("#frmTransgression button[type=submit]").ClickAsync();
        Assert.False(await page.Locator("#EmployeeId").EvaluateAsync<bool>("element => element.checkValidity()"));
        Assert.Equal(before, (await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning")).Single()["Total"]);
    });

    [Fact]
    public Task Editing_draft_persists_replacement_employee_and_category() => RunAsync("edit-draft", async page =>
    {
        var id = Database.Warnings["Draft"];
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).SelectEmployeeAsync("test-employee-bob");
        foreach (var checkbox in await page.Locator("input[name=CategoryIds]:checked").AllAsync()) await checkbox.UncheckAsync();
        await page.Locator($"input[name=CategoryIds][value='{Database.ConductCategory}']").CheckAsync();
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#frmTransgression button[type=submit]").ClickAsync(), r => r.Request.Method == "POST" && r.Url.Contains("/Transgression/CreateTransgression"));
        Assert.True(response.Ok);
        var row = Assert.Single(await Database.QueryAsync("SELECT EmployeeId,LastStatusChangedBy FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)));
        Assert.Equal("test-employee-bob", row["EmployeeId"]);
        Assert.Equal("test-user", row["LastStatusChangedBy"]);
        Assert.Equal(Database.ConductCategory, Convert.ToInt32(Assert.Single(await Database.QueryAsync("SELECT CategoryId FROM ws.WarningCategory WHERE WarningId=@Id", ("@Id", id)))["CategoryId"]));
    });

    [Fact]
    public Task Calendar_dates_match_persisted_dates_in_South_African_timezone() => RunAsync("save-dates", async page =>
    {
        var id = Database.Warnings["Draft"];
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(2);
        var calendar = page.Locator("#wsCalendar .datepicker-days");
        await Expect(calendar).ToBeVisibleAsync();
        await calendar.Locator("td.day:not(.old):not(.new):not(.disabled):not(.active)").First.ClickAsync();
        var timestamps = await calendar.Locator("td.day.active").EvaluateAllAsync<double[]>("cells => cells.map(cell => Number(cell.dataset.date))");
        var expected = timestamps.Select(t => DateTimeOffset.FromUnixTimeMilliseconds((long)t).UtcDateTime.ToString("yyyy-MM-dd")).Order().ToArray();
        Assert.NotEmpty(expected);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#wsSaveDatesBtn").ClickAsync(), r => r.Url.Contains("/Transgression/SaveAnswer") && r.Request.Method == "POST");
        Assert.True(response.Ok);
        var answer = Assert.Single(await Database.QueryAsync("SELECT AnswerJson FROM ws.WarningAnswer WHERE WarningId=@Id AND QuestionId=1", ("@Id", id)));
        using var json = JsonDocument.Parse((string)answer["AnswerJson"]!);
        Assert.Equal(expected, json.RootElement.GetProperty("dates").EnumerateArray().Select(d => d.GetString()).Order());
    });

    [Fact]
    public Task Rich_description_is_saved_once_and_visible_in_overview() => RunAsync("description", async page =>
    {
        var id = Database.Warnings["Draft"];
        const string description = "UI updated description: the incident occurred at the front desk during the morning shift.";
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(3);
        await page.Locator("#wsQuillDescription .ql-editor").FillAsync(description);
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#wsSaveDescriptionBtn").ClickAsync(), r => r.Url.Contains("/Transgression/SaveAnswers") && r.Request.Method == "POST");
        Assert.True(response.Ok);
        var answer = Assert.Single(await Database.QueryAsync("SELECT AnswerText FROM ws.WarningAnswer WHERE WarningId=@Id AND QuestionId=2", ("@Id", id)));
        Assert.Contains(description, (string)answer["AnswerText"]!);
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(6);
        await Expect(page.Locator(".ws-pane[data-pane='6']")).ToContainTextAsync(description);
    });

    [Fact]
    public Task Sop_not_applicable_is_saved_to_database() => RunAsync("sop-not-applicable", async page =>
    {
        var id = Database.Warnings["Draft"];
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(4);
        await page.Locator("#IsSopNonCompliance").CheckAsync();
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnSaveSop").ClickAsync(), r => r.Url.Contains("/Transgression/SaveAnswer") && r.Request.Method == "POST");
        Assert.True(response.Ok);
        using var json = JsonDocument.Parse((string)Assert.Single(await Database.QueryAsync("SELECT AnswerJson FROM ws.WarningAnswer WHERE WarningId=@Id AND QuestionId=3", ("@Id", id)))["AnswerJson"]!);
        Assert.True(json.RootElement.GetProperty("notApplicable").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("sopDocumentId").ValueKind);
    });

    [Fact]
    public Task Selected_sop_id_and_name_are_saved() => RunAsync("sop-selection", async page =>
    {
        var id = Database.Warnings["Draft"];
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(4);
        await page.Locator("#IsSopNonCompliance").UncheckAsync();
        await page.Locator(".ws-pane[data-pane='4'] .ts-control input").FillAsync("UI Safety");
        await page.Locator(".ts-dropdown .option[data-value='9001']").ClickAsync();
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnSaveSop").ClickAsync(), r => r.Url.Contains("/Transgression/SaveAnswer") && r.Request.Method == "POST");
        Assert.True(response.Ok);
        using var json = JsonDocument.Parse((string)Assert.Single(await Database.QueryAsync("SELECT AnswerJson FROM ws.WarningAnswer WHERE WarningId=@Id AND QuestionId=3", ("@Id", id)))["AnswerJson"]!);
        Assert.False(json.RootElement.GetProperty("notApplicable").GetBoolean());
        Assert.Equal(9001, json.RootElement.GetProperty("sopDocumentId").GetInt32());
        Assert.Contains("UI Safety SOP", json.RootElement.GetProperty("sopDocumentName").GetString());
    });

    [Fact]
    public Task Uploaded_attachment_metadata_and_downloaded_bytes_match() => RunAsync("attachment", async page =>
    {
        var id = Database.Warnings["Draft"];
        var bytes = Encoding.UTF8.GetBytes("UI evidence text, saved through the real warning form.");
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(5);
        await page.Locator("#attachmentName").FillAsync("UI proof");
        await page.Locator("#attachmentFile").SetInputFilesAsync(new FilePayload { Name = "ui-proof.txt", MimeType = "text/plain", Buffer = bytes });
        await Expect(page.Locator("#pendingPreview")).ToContainTextAsync("ui-proof.txt");
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnSaveAttachmentsContinue").ClickAsync(), r => r.Url.Contains("/Transgression/SaveEvidenceAttachment") && r.Request.Method == "POST");
        Assert.True(response.Ok);
        var evidence = Assert.Single(await Database.QueryAsync("SELECT * FROM ws.WarningEvidence WHERE WarningId=@Id", ("@Id", id)));
        Assert.Equal(bytes.Length, Convert.ToInt32(evidence["FileSizeBytes"]));
        Assert.Equal("test-user", evidence["UploadedBy"]);
        Assert.Contains("ui-proof.txt", (string)evidence["FileName"]!);
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(5);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("a[href*='DownloadAttachment']").First.ClickAsync());
        var path = Path.Combine(Path.GetTempPath(), $"warning-evidence-{Guid.NewGuid():N}.txt");
        try { await download.SaveAsAsync(path); Assert.Equal(bytes, await File.ReadAllBytesAsync(path)); }
        finally { File.Delete(path); }
    });

    [Fact]
    public Task Missing_required_description_blocks_submission() => RunAsync("incomplete-overview", async page =>
    {
        var id = Database.Warnings["Draft"];
        await Database.QueryAsync("UPDATE ws.WarningAnswer SET AnswerText=NULL,AnswerJson=NULL WHERE WarningId=@Id AND QuestionId=2", ("@Id", id));
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(6);
        await Expect(page.Locator("#btnMarkNew")).ToBeDisabledAsync();
        Assert.Equal("Draft", Assert.Single(await Database.QueryAsync("SELECT Status FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["Status"]);
    });

    [Fact]
    public Task Complete_draft_submits_to_legal_and_records_email() => RunAsync("submit-draft", async page =>
    {
        var id = Database.Warnings["Draft"];
        await new WarningWizardPage(page).OpenAsync(id);
        await new WarningWizardPage(page).OpenStepAsync(6);
        await Expect(page.Locator("#btnMarkNew")).ToBeEnabledAsync();
        var response = await page.RunAndWaitForResponseAsync(() => page.Locator("#btnMarkNew").ClickAsync(), r => r.Url.Contains("/Transgression/UpdateStatus") && r.Request.Method == "POST");
        Assert.True(response.Ok);
        var row = Assert.Single(await Database.QueryAsync("SELECT w.SubmittedOn,s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId WHERE WarningId=@Id", ("@Id", id)));
        Assert.Equal(2, Convert.ToInt32(row["IssueStatusGroup"]));
        Assert.NotNull(row["SubmittedOn"]);
        using var messages = JsonDocument.Parse(await (await page.Context.APIRequest.GetAsync("/__testing/outbox")).TextAsync());
        Assert.Contains(messages.RootElement.EnumerateArray(), mail => mail.GetProperty("subject").GetString()!.Contains($"#{id} Created") && mail.GetProperty("recipients").EnumerateArray().Any(to => to.GetString()=="legal@example.test"));
    });
}
