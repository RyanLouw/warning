namespace WarningSystems.Core.ViewModels;

public class EmployeeDashboardVm
{
    public string EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string Department { get; set; }
    public string TeamLeader { get; set; }
    public string Manager { get; set; }

    public bool HasAccess { get; set; }

    public int TotalWarnings { get; set; }
    public int DraftCount { get; set; }
    public int NewCount { get; set; }
    public int InProgressCount { get; set; }
    public int DueCount { get; set; }
    public int OverdueCount { get; set; }

    public DateTime? LastActionedDate { get; set; }
    public string CurrentState { get; set; }

    public List<EmployeeWarningCardVm> Warnings { get; set; } = new();
    public List<EmployeeWarningNoteVm> Notes { get; set; } = new();
}
