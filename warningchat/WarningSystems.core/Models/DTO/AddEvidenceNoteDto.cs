namespace WarningSystems.Models.DTO;

public class AddEvidenceNoteDto
{
    public long WarningId { get; set; }

    public long? EvidenceId { get; set; }

    public int NoteTypeId { get; set; }

    public string NoteText { get; set; } = string.Empty;
}