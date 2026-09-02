namespace WarningSystems.Core.ViewModels;

public class AddEvidenceNoteRequest
{
    public long WarningId { get; set; }
    public long? EvidenceId { get; set; }
    public string NoteText { get; set; } = "";
}