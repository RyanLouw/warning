using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using FakeItEasy;
using WarningSystems.Controllers;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.Manager;
using WarningSystems.Core.Models.Enum;
using WarningSystems.Core.ViewModels;
using WarningSystems.Models.DTO;
using WarningSystems.Services.Interface;

namespace WarningSystemTest.Unit;

[Trait("Category", "Unit")]
public class EndpointTests
{
    private readonly ITransgressionManager _manager = A.Fake<ITransgressionManager>(options => options.Strict());
    private readonly IAzureFileStorageDataAccess _storage = A.Fake<IAzureFileStorageDataAccess>(options => options.Strict());
    private LegalController Legal(bool named = true) => new(NullLogger<LegalController>.Instance, _manager, _storage) {
        ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(named ? new[] { new Claim(ClaimTypes.Name, "actor") } : [], "Test")) } }
    };
    private TransgressionController User() => new(NullLogger<HomeController>.Instance, _manager, _storage, A.Fake<IWarningPdfExportService>());

    [Theory]
    [InlineData(0, 1, "note")]
    [InlineData(1, 0, "note")]
    [InlineData(1, 1, " ")]
    public async Task Legal_note_rejects_invalid_input(long id, int type, string text)
    {
        Assert.IsType<BadRequestObjectResult>(await Legal().AddEvidenceNote(new() { WarningId = id, NoteTypeId = type, NoteText = text }));
        Assert.Empty(Fake.GetCalls(_manager));
    }

    [Fact]
    public async Task Legal_note_saves_valid_input()
    {
        A.CallTo(() => _manager.AddWarningNoteAsync(42, null, 1, "note")).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await Legal().AddEvidenceNote(new() { WarningId = 42, NoteTypeId = 1, NoteText = "note" }));
        A.CallTo(() => _manager.AddWarningNoteAsync(42, null, 1, "note")).MustHaveHappened();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Legal_visibility_handles_missing_issue(bool found)
    {
        A.CallTo(() => _manager.SetHideFromTeamLeadAsync(42, true, "actor")).Returns(found ? true : null);
        var result = await Legal().SetTeamLeadVisibility(new() { WarningId = 42, HideFromTeamLead = true });
        if (found) Assert.IsType<OkObjectResult>(result); else Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Visibility_requires_valid_id() => Assert.IsType<BadRequestObjectResult>(await Legal().SetTeamLeadVisibility(new()));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Legal_validation_returns_business_error(bool valid)
    {
        if (valid) A.CallTo(() => _manager.ValidateWarningAsync(42)).Returns(Task.CompletedTask);
        else A.CallTo(() => _manager.ValidateWarningAsync(42)).ThrowsAsync(new InvalidOperationException("Wrong stage"));
        var result = await Legal().ValidateTransgression(new() { WarningId = 42 });
        if (valid) Assert.IsType<OkObjectResult>(result); else Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Validation_requires_valid_id() => Assert.IsType<BadRequestObjectResult>(await Legal().ValidateTransgression(new()));

    [Fact]
    public async Task Due_date_update_requires_named_actor() => Assert.IsType<UnauthorizedObjectResult>(await Legal(false).UpdateDueDate(new() { WarningId = 42 }));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Due_date_update_returns_business_result(bool success)
    {
        var dto = new UpdateDueDateDto { WarningId = 42 };
        A.CallTo(() => _manager.UpdateDueDateWithAuditAsync(dto, "actor")).Returns((success, "message"));
        var result = await Legal().UpdateDueDate(dto);
        if (success) Assert.IsType<OkObjectResult>(result); else Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task Completion_requires_warning_and_type(int warning, int type) =>
        Assert.IsType<BadRequestObjectResult>(await Legal().CompleteTransgression(new() { WarningId = warning, IssueTypeId = type }));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Completion_sends_email_only_after_successful_decision(bool success)
    {
        var dto = new CompleteTransgressionDto { WarningId = 42, IssueTypeId = 2, IssueSubTypeId = 1 };
        var decisionCompleted = false;
        if (success)
        {
            A.CallTo(() => _manager.ApplyLegalDecisionAsync(42, 2, 1))
                .ReturnsLazily(async () => { await Task.Yield(); decisionCompleted = true; });
            A.CallTo(() => _manager.SendEmailToTeamLeadAsync(dto))
                .Invokes(() => Assert.True(decisionCompleted)).Returns(Task.CompletedTask);
        }
        else A.CallTo(() => _manager.ApplyLegalDecisionAsync(42, 2, 1)).ThrowsAsync(new InvalidOperationException("Wrong stage"));
        var result = await Legal().CompleteTransgression(dto);
        if (success)
        {
            Assert.IsType<OkObjectResult>(result);
            A.CallTo(() => _manager.ApplyLegalDecisionAsync(42, 2, 1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _manager.SendEmailToTeamLeadAsync(dto)).MustHaveHappenedOnceExactly();
        }
        else {
            Assert.IsType<BadRequestObjectResult>(result);
            A.CallTo(() => _manager.SendEmailToTeamLeadAsync(A<CompleteTransgressionDto>._)).MustNotHaveHappened();
        }
    }

    [Theory]
    [InlineData(0, "message")]
    [InlineData(1, " ")]
    public async Task Information_request_requires_id_and_message(long id, string message) =>
        Assert.IsType<BadRequestObjectResult>(await Legal().RequestTeamLeadInformation(new() { WarningId = id, Message = message }));

    [Fact]
    public async Task Information_request_limits_message_length() =>
        Assert.IsType<BadRequestObjectResult>(await Legal().RequestTeamLeadInformation(new() { WarningId = 42, Message = new string('x', 1001) }));

    [Fact]
    public async Task Information_request_limits_attachment_size()
    {
        var file = A.Fake<IFormFile>();
        A.CallTo(() => file.Length).Returns(3 * 1024 * 1024 + 1);
        Assert.IsType<BadRequestObjectResult>(await Legal().RequestTeamLeadInformation(new() { WarningId = 42, Message = "message", File = file }));
        Assert.Empty(Fake.GetCalls(_manager));
    }

    [Fact]
    public async Task Valid_information_request_is_trimmed()
    {
        A.CallTo(() => _manager.SendMoreInformationRequiredEmailAsync(42, "message", null)).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await Legal().RequestTeamLeadInformation(new() { WarningId = 42, Message = " message " }));
        A.CallTo(() => _manager.SendMoreInformationRequiredEmailAsync(42, "message", null)).MustHaveHappened();
    }

    [Fact]
    public async Task Failed_information_request_does_not_expose_internal_exception()
    {
        A.CallTo(() => _manager.SendMoreInformationRequiredEmailAsync(42, "message", null)).ThrowsAsync(new Exception("internal details"));
        var result = Assert.IsType<ObjectResult>(await Legal().RequestTeamLeadInformation(new() { WarningId = 42, Message = "message" }));
        Assert.Equal(500, result.StatusCode);
        Assert.DoesNotContain("internal details", System.Text.Json.JsonSerializer.Serialize(result.Value));
    }

    [Fact]
    public async Task Legal_index_rejects_draft()
    {
        A.CallTo(() => _manager.GetWarningLegalAsync(42)).Returns(new LegalWizardVm { IssueStatusGroup = IssueStatusGroup.Draft });
        Assert.IsType<ForbidResult>(await Legal().Index(42));
    }

    [Fact]
    public async Task Legal_index_marks_new_issue_in_progress_and_reloads()
    {
        A.CallTo(() => _manager.GetWarningLegalAsync(42)).ReturnsNextFromSequence(
            Task.FromResult(new LegalWizardVm { IssueStatusGroup = IssueStatusGroup.LegalReview }),
            Task.FromResult(new LegalWizardVm { IssueStatusGroup = IssueStatusGroup.LegalDecision }));
        A.CallTo(() => _manager.MarkWarningInProgressAsync(42)).Returns(Task.CompletedTask);
        Assert.IsType<ViewResult>(await Legal().Index(42));
        A.CallTo(() => _manager.GetWarningLegalAsync(42)).MustHaveHappenedTwiceExactly();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task Saving_answer_requires_warning_and_question(long warning, int question) =>
        Assert.IsType<BadRequestObjectResult>(await User().SaveAnswer(new() { WarningId = warning, QuestionId = question }));

    [Fact]
    public async Task Answer_batch_cannot_mix_warning_ids()
    {
        Assert.IsType<BadRequestObjectResult>(await User().SaveAnswers([new() { WarningId = 1, QuestionId = 1 }, new() { WarningId = 2, QuestionId = 2 }]));
        Assert.Empty(Fake.GetCalls(_manager));
    }

    [Fact]
    public async Task Empty_answer_batch_is_rejected() => Assert.IsType<BadRequestObjectResult>(await User().SaveAnswers([]));

    [Fact]
    public async Task Valid_answer_batch_saves_all_answers()
    {
        var answers = new List<SaveAnswerDto> { new() { WarningId = 42, QuestionId = 1 }, new() { WarningId = 42, QuestionId = 2 } };
        foreach (var answer in answers) A.CallTo(() => _manager.SaveAnswerAsync(answer)).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await User().SaveAnswers(answers));
        foreach (var answer in answers)
            A.CallTo(() => _manager.SaveAnswerAsync(answer)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(0, "New", null)]
    [InlineData(1, "", null)]
    [InlineData(1, "Completed", null)]
    [InlineData(1, "New", "not-a-date")]
    public async Task Submission_rejects_invalid_requests(long warning, string status, string? due) =>
        Assert.IsType<BadRequestObjectResult>(await User().UpdateStatus(new() { WarningId = warning, Status = status, DueDate = due }));

    [Fact]
    public async Task Missing_legal_download_returns_404()
    {
        A.CallTo(() => _storage.GetFileStreamAsync(42, "missing.pdf")).ThrowsAsync(new FileNotFoundException());
        Assert.IsType<NotFoundResult>(await Legal().Download(42, "missing.pdf"));
    }

    [Fact]
    public async Task Missing_user_download_returns_404()
    {
        A.CallTo(() => _storage.GetFileAsync(42, "missing.pdf")).ThrowsAsync(new FileNotFoundException());
        Assert.IsType<NotFoundResult>(await User().DownloadAttachment(42, "missing.pdf"));
    }
}
