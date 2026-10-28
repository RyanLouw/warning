using System.Text;
using System.Text.Json;
using WarningSystemTest.Fixtures;
using WarningSystemTest.Models;

namespace WarningSystemTest.Tests.Integration;

[Collection("Warning browser")]
[Trait("Category", "Integration")]
public sealed class HttpTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Theory]
    [InlineData("User","/Admin/Index",403)]
    [InlineData("Legal","/Admin/Index",403)]
    [InlineData("Chairperson","/Admin/Index",403)]
    [InlineData("Admin","/Admin/Index",200)]
    [InlineData("Legal","/Transgression/Index",403)]
    [InlineData("Admin","/Transgression/Index",403)]
    [InlineData("User","/Legal/Index/1",403)]
    [InlineData("User","/Home/Index",200)]
    [InlineData("Legal","/Home/Index",200)]
    public async Task Routes_enforce_real_authorization(string role,string url,int expected)
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole(role));
        Assert.Equal(expected,(await context.APIRequest.GetAsync(url)).Status);
    }

    [Theory]
    [InlineData("/Home/Index")]
    [InlineData("/Admin/Index")]
    [InlineData("/Transgression/Index")]
    public async Task Anonymous_requests_are_rejected(string url)
    {
        await using var context=await Browser.CreateContextAsync(null);
        Assert.Equal(401,(await context.APIRequest.GetAsync(url)).Status);
    }

    [Fact]
    public async Task Antiforgery_rejection_does_not_create_a_discussion()
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("User"));
        var before=Assert.Single(await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning"))["Total"];
        var response=await context.APIRequest.PostAsync("/Home/CreateAbsenceDiscussion",new() { DataObject=new { EmployeeId="test-employee-alice", IssueSubTypeId=Database.DiscussionSubtype, Dates=new[]{"2026-01-01"}, Description="must not be saved" } });
        Assert.Equal(400,response.Status);
        Assert.Equal(before,Assert.Single(await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning"))["Total"]);
    }

    [Theory]
    [InlineData(0,1)]
    [InlineData(1,0)]
    public async Task Invalid_answer_identifiers_return_bad_request(long warning,int question)
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("User"));
        Assert.Equal(400,(await context.APIRequest.PostAsync("/Transgression/SaveAnswer",new(){DataObject=new { WarningId=warning,QuestionId=question,AnswerText="invalid" }})).Status);
    }

    [Fact]
    public async Task Answer_batches_cannot_mix_warning_ids()
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("User"));
        var first=Database.Warnings["Draft"];var second=Database.Warnings["New"];
        var response=await context.APIRequest.PostAsync("/Transgression/SaveAnswers",new(){DataObject=new[]{new{WarningId=first,QuestionId=2,AnswerText="invalid batch"},new{WarningId=second,QuestionId=2,AnswerText="invalid batch"}}});
        Assert.Equal(400,response.Status);
        Assert.Empty(await Database.QueryAsync("SELECT AnswerId FROM ws.WarningAnswer WHERE WarningId IN (@First,@Second) AND AnswerText='invalid batch'",("@First",first),("@Second",second)));
    }

    [Fact]
    public async Task Saving_an_answer_twice_updates_one_database_row()
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("User"));
        var id=Database.Warnings["Draft"];
        foreach(var text in new[]{"UI first answer","UI updated answer"})
            Assert.Equal(200,(await context.APIRequest.PostAsync("/Transgression/SaveAnswer",new(){DataObject=new{WarningId=id,QuestionId=2,AnswerText=text}})).Status);
        Assert.Equal("UI updated answer",Assert.Single(await Database.QueryAsync("SELECT AnswerText FROM ws.WarningAnswer WHERE WarningId=@Id AND QuestionId=2",("@Id",id)))["AnswerText"]);
    }

    [Fact]
    public async Task Legal_cannot_open_a_draft()
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("Legal"));
        Assert.Equal(403,(await context.APIRequest.GetAsync($"/Legal/Index/{Database.Warnings["Draft"]}")).Status);
    }

    [Fact]
    public async Task Missing_legal_issue_returns_not_found()
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("Legal"));
        Assert.Equal(404,(await context.APIRequest.GetAsync("/Legal/Index/9223372036854775807")).Status);
    }

    [Fact]
    public async Task Invalid_legal_decision_does_not_advance_status()
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("Legal"));
        var id=Database.Warnings["Decision"];
        var response=await context.APIRequest.PostAsync("/Legal/CompleteTransgression",new(){Form=context.APIRequest.CreateFormData().Set("WarningId",id.ToString()).Set("IssueTypeId","0")});
        Assert.Equal(400,response.Status);
        Assert.Equal("In Progress",Assert.Single(await Database.QueryAsync("SELECT Status FROM ws.Warning WHERE WarningId=@Id",("@Id",id)))["Status"]);
    }

    [Fact]
    public async Task Draft_cannot_be_validated()
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("Legal"));
        var id=Database.Warnings["Draft"];
        Assert.Equal(400,(await context.APIRequest.PostAsync("/Legal/ValidateTransgression",new(){DataObject=new{WarningId=id}})).Status);
        Assert.Equal("Draft",Assert.Single(await Database.QueryAsync("SELECT Status FROM ws.Warning WHERE WarningId=@Id",("@Id",id)))["Status"]);
    }

    [Fact]
    public async Task PDF_export_returns_a_valid_download()
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole("User"));
        var response=await context.APIRequest.GetAsync($"/Transgression/ExportOverviewPdf?warningId={Database.Warnings["Draft"]}");
        Assert.Equal(200,response.Status);
        Assert.StartsWith("application/pdf",response.Headers["content-type"]);
        var bytes=await response.BodyAsync();
        Assert.StartsWith("%PDF-",Encoding.ASCII.GetString(bytes[..8]));
        Assert.True(bytes.Length>500);
    }

    [Theory]
    [InlineData("User","Draft","/Transgression/Index/")]
    [InlineData("User","Issued","/Transgression/TeamLeadWarning/")]
    [InlineData("Legal","Issued","/Legal/Index/")]
    public async Task Email_redirects_lead_to_correct_role_screen(string role,string status,string expected)
    {
        await using var context=await Browser.CreateContextAsync(TestUsers.ForRole(role));
        var response=await context.APIRequest.GetAsync($"/Home/Redirect?warningId={Database.Warnings[status]}&status={status}",new(){MaxRedirects=0});
        Assert.Equal(302,response.Status);
        Assert.StartsWith(expected,response.Headers["location"]);
    }
}
