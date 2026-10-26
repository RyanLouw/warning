using Moq;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;

namespace WarningSystems.UnitTests;

public class WorkflowTests
{
    private readonly ManagerFixture _f = new();

    [Theory]
    [InlineData(IssueStatusGroup.Draft)]
    [InlineData(IssueStatusGroup.LegalReview)]
    [InlineData(IssueStatusGroup.LegalDecision)]
    [InlineData(IssueStatusGroup.EmployeeAction)]
    [InlineData(IssueStatusGroup.Closed)]
    public async Task Validation_rejects_non_issued_issues(IssueStatusGroup group)
    {
        WarningIn(group);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.ValidateWarningAsync(42));
        _f.Data.Verify(x => x.AdvanceWarningStatusAsync(It.IsAny<long>(), It.IsAny<IssueStatusGroup>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Issued_issue_can_be_validated()
    {
        WarningIn(IssueStatusGroup.LegalValidation);
        _f.Data.Setup(x => x.AdvanceWarningStatusAsync(42, IssueStatusGroup.LegalValidation, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.ValidateWarningAsync(42);
        _f.Data.VerifyAll();
    }

    [Fact]
    public async Task Missing_issue_cannot_be_validated()
    {
        _f.Data.Setup(x => x.GetWarningByIdAsync(42)).ReturnsAsync((Warning?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.Manager.ValidateWarningAsync(42));
    }

    [Theory]
    [InlineData(IssueStatusGroup.LegalReview)]
    [InlineData(IssueStatusGroup.EmployeeAction)]
    [InlineData(IssueStatusGroup.Closed)]
    public async Task Only_drafts_can_be_submitted(IssueStatusGroup group)
    {
        WarningIn(group);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.UpdateWarningStatusAsync(42, null, "New"));
        _f.Data.Verify(x => x.UpdateWarningDueDateAsync(It.IsAny<long>(), It.IsAny<DateOnly?>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Draft_submission_advances_status_and_updates_due_date()
    {
        WarningIn(IssueStatusGroup.Draft);
        var date = new DateOnly(2027, 1, 4);
        _f.Data.Setup(x => x.AdvanceWarningStatusAsync(42, IssueStatusGroup.Draft, "actor")).Returns(Task.CompletedTask);
        _f.Data.Setup(x => x.UpdateWarningDueDateAsync(42, date, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.UpdateWarningStatusAsync(42, date, "New");
        _f.Data.VerifyAll();
    }

    [Fact]
    public async Task Due_date_only_change_does_not_submit()
    {
        _f.Data.Setup(x => x.UpdateWarningDueDateAsync(42, null, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.UpdateWarningStatusAsync(42, null, null);
        _f.Data.VerifyAll();
    }

    [Theory]
    [InlineData(IssueStatusGroup.Draft)]
    [InlineData(IssueStatusGroup.LegalReview)]
    [InlineData(IssueStatusGroup.Closed)]
    public async Task Decision_requires_ready_status(IssueStatusGroup group)
    {
        WarningIn(group);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.ApplyLegalDecisionAsync(42, 2, null));
    }

    [Theory]
    [InlineData(IssueStatusGroup.LegalDecision)]
    [InlineData(IssueStatusGroup.AddedMoreInformation)]
    public async Task Decision_is_allowed_when_ready(IssueStatusGroup group)
    {
        WarningIn(group);
        var type = new LookupIssueType { IssueTypeId = 2, SubTypeSelectionMode = SubTypeSelectionMode.Required, IssueSubTypes = [new() { IssueSubTypeId = 3 }] };
        _f.Data.Setup(x => x.GetActiveIssueTypesAsync()).ReturnsAsync([type]);
        _f.Data.Setup(x => x.UpdateWarningDecisionAsync(42, type, type.IssueSubTypes.Single(), "actor")).Returns(Task.CompletedTask);
        await _f.Manager.ApplyLegalDecisionAsync(42, 2, 3);
        _f.Data.VerifyAll();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(999)]
    public async Task Required_subtype_must_belong_to_selected_type(int? subtype)
    {
        WarningIn(IssueStatusGroup.LegalDecision);
        _f.Data.Setup(x => x.GetActiveIssueTypesAsync()).ReturnsAsync([new() { IssueTypeId = 2, SubTypeSelectionMode = SubTypeSelectionMode.Required }]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.ApplyLegalDecisionAsync(42, 2, subtype));
    }

    [Fact]
    public async Task Unknown_issue_type_is_rejected()
    {
        WarningIn(IssueStatusGroup.LegalDecision);
        _f.Data.Setup(x => x.GetActiveIssueTypesAsync()).ReturnsAsync([]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.ApplyLegalDecisionAsync(42, 999, null));
    }

    [Fact]
    public async Task Types_without_subtypes_ignore_subtype_input()
    {
        WarningIn(IssueStatusGroup.LegalDecision);
        var type = new LookupIssueType { IssueTypeId = 1, SubTypeSelectionMode = SubTypeSelectionMode.None };
        _f.Data.Setup(x => x.GetActiveIssueTypesAsync()).ReturnsAsync([type]);
        _f.Data.Setup(x => x.UpdateWarningDecisionAsync(42, type, null, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.ApplyLegalDecisionAsync(42, 1, 99);
        _f.Data.VerifyAll();
    }

    [Fact]
    public async Task Mark_in_progress_requires_nonzero_id() =>
        await Assert.ThrowsAsync<ArgumentException>(() => _f.Manager.MarkWarningInProgressAsync(0));

    [Fact]
    public async Task Mark_in_progress_advances_legal_review()
    {
        _f.Data.Setup(x => x.AdvanceWarningStatusAsync(42, IssueStatusGroup.LegalReview, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.MarkWarningInProgressAsync(42);
        _f.Data.VerifyAll();
    }

    private void WarningIn(IssueStatusGroup group) =>
        _f.Data.Setup(x => x.GetWarningByIdAsync(42)).ReturnsAsync(new Warning { WarningId = 42, IssueStatus = new() { IssueStatusGroup = group } });
}
