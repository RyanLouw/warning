using Microsoft.Graph.Models;
using Moq;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;
using WarningSystems.Core.ViewModels;
using WarningSystems.Models.DTO;

namespace WarningSystems.UnitTests;

public class EmailTests
{
    [Fact]
    public async Task Legal_submission_email_contains_warning_data_all_categories_and_encoded_names()
    {
        var f = Setup();
        Message? sent = null;
        f.Graph.Setup(x => x.SendEmailAsync(It.IsAny<Message>(), true)).Callback<Message, bool>((m, _) => sent = m).Returns(Task.CompletedTask);
        await f.Manager.SendEmailToLegalAsync(42, new DateOnly(2027, 1, 4), " New ");
        Assert.NotNull(sent);
        Assert.Equal("Warning System | Issue #42 Created", sent.Subject);
        Assert.Contains("&lt;Employee&gt;", sent.Body!.Content);
        Assert.Contains("Attendance, Safety", sent.Body.Content);
        Assert.Contains("04/01/2027", sent.Body.Content);
        Assert.Contains("https://example.test/Legal/Index/42", sent.Body.Content);
        Assert.DoesNotContain("<Employee>", sent.Body.Content);
        Assert.Single(sent.ToRecipients!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Team_lead_email_targets_creator_and_preserves_optional_attachment(bool attach)
    {
        var f = Setup();
        SendEmailRequest? sent = null;
        f.Graph.Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>())).Callback<SendEmailRequest>(m => sent = m).Returns(Task.CompletedTask);
        var dto = new CompleteTransgressionDto { WarningId = 42 };
        if (attach) dto.File = new Microsoft.AspNetCore.Http.FormFile(new MemoryStream([1, 2]), 0, 2, "file", "decision.pdf") { Headers = new Microsoft.AspNetCore.Http.HeaderDictionary(), ContentType = "application/pdf" };
        await f.Manager.SendEmailToTeamLeadAsync(dto);
        Assert.NotNull(sent);
        Assert.Equal(new[] { "actor@example.test" }, sent.ToRecipients);
        Assert.Contains("https://example.test/Transgression/TeamLeadWarning/42", sent.BodyHtml);
        Assert.Contains("&lt;Employee&gt;", sent.BodyHtml);
        Assert.Equal(attach ? 1 : 0, sent.Attachments.Count);
        if (attach) Assert.Equal(new byte[] { 1, 2 }, sent.Attachments[0].ContentBytes);
    }

    [Fact]
    public async Task Completed_issue_advances_employee_action_and_notifies_legal()
    {
        var f = Setup();
        f.Data.Setup(x => x.AdvanceWarningStatusAsync(42, IssueStatusGroup.EmployeeAction, "actor")).Returns(Task.CompletedTask);
        SendEmailRequest? sent = null;
        f.Graph.Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>())).Callback<SendEmailRequest>(m => sent = m).Returns(Task.CompletedTask);
        await f.Manager.NotifyLegalIssueCompletedAsync(42);
        Assert.NotNull(sent);
        Assert.Contains("Legal action required", sent.Subject);
        Assert.Contains("Attendance, Safety", sent.BodyHtml);
        Assert.Contains("https://example.test/Legal/Index/42", sent.BodyHtml);
        f.Data.Verify(x => x.AdvanceWarningStatusAsync(42, IssueStatusGroup.EmployeeAction, "actor"), Times.Once);
    }

    [Fact]
    public async Task Missing_warning_does_not_send_legal_email()
    {
        var f = new ManagerFixture();
        f.Data.Setup(x => x.GetWarningByIdAsync(42)).ReturnsAsync((Warning?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Manager.SendEmailToLegalAsync(42, null, null));
        f.Graph.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Missing_creator_email_is_reported_with_context()
    {
        var f = Setup();
        f.Graph.Setup(x => x.GetUserAsync("actor")).ReturnsAsync(new User { Id = "actor", DisplayName = "Actor" });
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Manager.SendEmailToTeamLeadAsync(new() { WarningId = 42 }));
        Assert.Contains("Creator has no Mail/UPN", error.Message);
        f.Graph.Verify(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>()), Times.Never);
    }

    [Fact]
    public async Task Completion_requires_warning_id()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new ManagerFixture().Manager.NotifyLegalIssueCompletedAsync(0));
        Assert.IsType<ArgumentException>(error.InnerException);
    }

    private static ManagerFixture Setup()
    {
        var f = new ManagerFixture();
        f.EmailSettings.LegalRecipients = [" legal@example.test ", "LEGAL@example.test"];
        f.Data.Setup(x => x.GetWarningByIdAsync(42)).ReturnsAsync(new Warning { WarningId = 42, EmployeeId = "employee", CreatedBy = "actor", Status = "New", Type = "Warning", CategoryId = 1, Category = new() { Name = "Safety" } });
        f.Data.Setup(x => x.GetWarningCategoriesByWarningIdAsync(42)).ReturnsAsync([
            new() { CategoryId = 1, Category = new() { Name = "Safety" } },
            new() { CategoryId = 2, Category = new() { Name = "Attendance" } }
        ]);
        f.Graph.Setup(x => x.GetUserAsync("employee")).ReturnsAsync(new User { Id = "employee", DisplayName = "<Employee>" });
        f.Graph.Setup(x => x.GetUserAsync("actor")).ReturnsAsync(new User { Id = "actor", DisplayName = "Actor", Mail = "actor@example.test" });
        return f;
    }
}
