namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class WarningNote
{
    public long NoteId { get; set; }

    public long WarningId { get; set; }

    public long? EvidenceId { get; set; }

    public string NoteText { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public int? NoteTypeId { get; set; }

    public Warning Warning { get; set; } = null!;

    public WarningEvidence? Evidence { get; set; }

    public NoteTypeLookup? NoteType { get; set; }
}