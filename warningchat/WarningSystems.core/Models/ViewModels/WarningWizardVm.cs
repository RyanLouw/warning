using Microsoft.Graph.Models;

namespace WarningSystems.Core.ViewModels;

public class WarningWizardVm
{
    public long? WarningId { get; set; }
    public bool IsNew => !WarningId.HasValue || WarningId.Value <= 0;

    public List<User> UnderMe { get; set; } = [];
    public  string EmployeeId { get; set; }
    public  string EmployeeName { get; set; }

    public List<int> CategoryIds { get; set; } = [];
    public string Status { get; set; } = "Draft";

    public DateTime? CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? SubmittedOnUtc { get; set; }

    public DateOnly? LegalExpiryDate { get; set; }
    public DateTime? LastStatusChangedOnUtc { get; set; }
    public string? LastStatusChangedBy { get; set; }

    public List<string> CategoryNames { get; set; } = [];

    public List<CategoryVM> Categories { get; set; } = [];

    public List<WarningQuestionVm>? Questions { get; set; } = [];
    public List<WarningEvidenceVm>? Evidence { get; set; } = [];
    public List<WarningNoteVm> Notes { get; set; } = [];

    public bool IsSopNonCompliance { get; set; }
    public int? SelectedSOPDocumentId { get; set; }
    public List<SopDocumentLiteVm> SOPs { get; set; } = [];
    public List<string> EvidenceStored { get; set; } = [];

    public string EvidenceNotes { get; set; } = "";
    public bool? HasAccess { get; set; }
}
