namespace WarningSystems.Core.ViewModels;

public class TransgretionsVM
{
    public long WarningId { get; set; }

    public required string EmployeeId { get; set; }
    public required string CreatedBy { get; set; }

    public required string EmployeeDisplayName { get; set; }
    public string CreatedByDisplayName { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public required string Status { get; set; }
    public string Type { get; set; } = "Issue";
    public string? WarningSubtype { get; set; }

    public int CategoryId { get; set; }

    public List<int> CategoryIds { get; set; } = [];
    public List<string> CategoryNames { get; set; } = [];

    public string Department { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public string TeamLeader { get; set; } = string.Empty;

    public string IssueType { get; set; } = string.Empty;
    public string IssueTypeName { get; set; } = string.Empty;
    public string IssueSubTypeName { get; set; } = string.Empty;
    public bool IsAbsenceDiscussion { get; set; }

    public string LatestNoteType { get; set; } = string.Empty;
    public string LatestNoteText { get; set; } = string.Empty;
    public DateTime? LatestNoteDate { get; set; }

    public bool IssuedToEmployee { get; set; }

    public DateTime? SubmittedOn { get; set; }
    public string? LegalNotes { get; set; }
    public DateOnly? LegalExpiryDate { get; set; }

    public DateTime? LastStatusChangedOn { get; set; }
    public string? LastStatusChangedBy { get; set; }

    public bool HideFromTeamLead { get; set; }
}
