namespace WarningSystems.Core.ViewModels;

public class EmployeeWarningNoteVm
{
    public long WarningId { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = "";
    public string CreatedByDisplayName { get; set; } = "";
    public string NoteType { get; set; } = "";
    public string NoteText { get; set; } = "";
}