namespace WarningSystems.Core.ViewModels;

public class LegalWarningHistoryVm
{
    public long WarningId { get; set; }
    public DateTime CreatedOn { get; set; }
    public string Status { get; set; } = "";
    public DateTime? LastStatusChangedOn { get; set; }
    public string? LastStatusChangedBy { get; set; }
    public string? LastStatusChangedByDisplayName { get; set; }
    public string CategoryName { get; set; } = "";
}
