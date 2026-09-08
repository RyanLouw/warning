using WarningSystems.core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class Warning : IAutomaticallyAuditedEntity
{
    public long WarningId { get; set; }
    public string EmployeeId { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }

    public string Status { get; set; }

    public string Type { get; set; } = "Issue";
    public string? WarningSubtype { get; set; }

    public int? IssueStatusId { get; set; }
    public int? IssueTypeId { get; set; }
    public int? IssueSubTypeId { get; set; }

    public int CategoryId { get; set; }

    public DateTime? SubmittedOn { get; set; }

    public DateOnly? LegalExpiryDate { get; set; }

    public DateTime? LastStatusChangedOn { get; set; }
    public string? LastStatusChangedBy { get; set; }

    //public bool Completed { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public bool HideFromTeamLead { get; set; } = false;

    public TransgressionCategory? Category { get; set; }
    public ICollection<WarningAnswer> Answers { get; set; } = new List<WarningAnswer>();
    public ICollection<WarningEvidence> Evidence { get; set; } = new List<WarningEvidence>();
    public ICollection<WarningCategory> WarningCategories { get; set; } = new List<WarningCategory>();
    public ICollection<WarningNote> Notes { get; set; } = new List<WarningNote>();
    public LookupIssueStatus? IssueStatus { get; set; }
    public LookupIssueType? IssueType { get; set; }
    public LookupIssueSubType? IssueSubType { get; set; }
}
