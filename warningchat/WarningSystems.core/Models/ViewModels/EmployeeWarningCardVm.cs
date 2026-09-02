namespace WarningSystems.Core.ViewModels;

public class EmployeeWarningCardVm
{
    public long WarningId { get; set; }
    public string Status { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string CreatedByDisplayName { get; set; } = "";
    public DateTime CreatedOn { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? LastActionedDate { get; set; }
    public string DescriptionSummary { get; set; } = "";
}
