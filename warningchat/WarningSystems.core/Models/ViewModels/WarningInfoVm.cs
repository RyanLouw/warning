namespace WarningSystems.Core.ViewModels;

public class WarningInfoVm
{
    public long WarningId { get; set; }
    public required string EmployeeId { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }

    public required string Status { get; set; }

    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public DateTime? SubmittedOn { get; set; }
    public DateOnly? LegalExpiryDate { get; set; }

    public DateTime? LastStatusChangedOn { get; set; }
    public string? LastStatusChangedBy { get; set; }
}