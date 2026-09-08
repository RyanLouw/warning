using WarningSystems.Core.Models.Enum;

namespace WarningSystems.Core.ViewModels;

public class LegalWizardVm
{
    public long WarningId { get; set; }
    public  string EmployeeId { get; set; }
    public  string Status { get; set; }
    public IssueStatusGroup IssueStatusGroup { get; set; }
    public string Type { get; set; } = "Issue";
    public string? WarningSubtype { get; set; }
    public DateTime CreatedOn { get; set; }
    public  string CreatedBy { get; set; }

    public  string EmployeeIdDesplayName { get; set; }

    public  string CreatedByDesplayName { get; set; }
    public int CategoryId { get; set; }
    public  string CategoryName { get; set; }

    public DateTime? SubmittedOn { get; set; }
    public DateOnly? LegalExpiryDate { get; set; }

    public bool HideFromTeamLead { get; set; }
    public List<WarningNoteVm> Notes { get; set; } = [];
    public List<LegalQuestionVm> Questions { get; set; } = [];
    public List<LegalEvidenceVm> Evidence { get; set; } = [];
    public List<LegalWarningHistoryVm> TransgretionHistory { get; set; } = [];
    public List<NoteTypeLookupVm> NoteTypes { get; set; } = [];
    public List<IssueTypeLookupVm> IssueTypes { get; set; } = [];
}
