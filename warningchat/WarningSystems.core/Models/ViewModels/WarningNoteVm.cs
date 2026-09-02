namespace WarningSystems.Core.ViewModels;

public class WarningNoteVm
{
    public long NoteId { get; set; }

    public long WarningId { get; set; }

    public long? EvidenceId { get; set; }

    public int? NoteTypeId { get; set; }

    public string NoteTypeName { get; set; } = "";

    public string NoteText { get; set; } = "";

    public string CreatedBy { get; set; } = "";

    public string CreatedByDisplayName { get; set; } = "";

    public DateTime CreatedOnUtc { get; set; }
}
