namespace WarningSystems.Core.ViewModels;

public class EmployeeRowVm
{
    public required string EmployeeId { get; set; }
    public required string EmployeeName { get; set; }
    public required string Department { get; set; }

    public required string TeamLeader { get; set; }
    public required string Manager { get; set; }

    public int NoWarnings { get; set; }
    public DateTime? LastActionedDate { get; set; }
    public required string CurrentState { get; set; }
}