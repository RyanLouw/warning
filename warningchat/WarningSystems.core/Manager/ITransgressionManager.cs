using Microsoft.AspNetCore.Http;
using WarningSystems.Core.Models.Enum;
using WarningSystems.Core.ViewModels;
using WarningSystems.Models.DTO;
using WarningSystems.Models.Validation;

namespace WarningSystems.Core.Manager;

public interface ITransgressionManager
{
    public Task<WarningRedirectTarget> RedirectPicker(string status);

    public Task<TaskListVM> BuildTaskListAsync();

    public Task<EmployeeDashboardVm> GetEmployeeDashboardAsync(string employeeId);

    public Task<AdminVM> BuildAdminView();

    public Task<int> CreateQuestionAsync(CreateQuestionDto dto);

    public Task UpdateQuestionAsync(UpdateQuestionDto dto);

    public Task<int> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);

    public Task UpdateCategoryAsync(UpdateCategoryDto dto, CancellationToken cancellationToken = default);

    public Task<List<QuestionVM>> GetAvailableQuestionsAsync(int categoryId);

    public Task<List<LinkedQuestionRowVm>> GetLinkedQuestionsAsync(int categoryId);

    public Task SetCategoryQuestionLinkAsync(SetCategoryQuestionLinkDto dto);

    public Task<WarningWizardVm> GetWarningWizardAsync(long? id);

    public Task<LegalWizardVm?> GetWarningLegalAsync(long warningId);

    public Task MarkWarningInProgressAsync(long warningId);

    public Task<SaveIssueStepResult> SaveIssueStepAsync(CreateTransgressionDTO dto);

    public Task<SaveIssueStepResult> CreateAbsenceDiscussionAsync(CreateAbsenceDiscussionDto dto);

    public Task SaveAnswerAsync(SaveAnswerDto dto);

    public Task AddWarningNoteAsync(long warningId, long? evidenceId, int noteTypeId, string noteText);

    public Task<long?> SaveEvidenceAsync(long warningId, string? fileName, string? mediaType, long? fileSizeBytes, string? notes, int? typeid, bool leagle);

    public Task UpdateWarningStatusAsync(long warningId, DateOnly? duedate, string? status);
    public Task ApplyLegalDecisionAsync(long warningId, int issueTypeId, int? issueSubTypeId);
    public Task ValidateWarningAsync(long warningId);

    public Task SendEmailToLegalAsync(long warningId, DateOnly? duedate, string? status);

    public Task<(bool Success, string? Message)> UpdateDueDateWithAuditAsync(UpdateDueDateDto dto, string changedBy);

    public Task SendEmailToTeamLeadAsync(CompleteTransgressionDto dto);

    public Task SendMoreInformationRequiredEmailAsync(long warningId, string noteText, IFormFile? file = null);

    public Task SendMoreInformationAddedEmailAsync(long warningId, string? noteText, IReadOnlyCollection<IFormFile>? files = null);

    public Task AddTeamLeadEvidenceAsync(AddTeamLeadEvidenceVm model);

    public Task NotifyLegalIssueCompletedAsync(long warningId);
    Task<bool?> SetHideFromTeamLeadAsync(long warningId, bool hideFromTeamLead, string changedBy);
}
