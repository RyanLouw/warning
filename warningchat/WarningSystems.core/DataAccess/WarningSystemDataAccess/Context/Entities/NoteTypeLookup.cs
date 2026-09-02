namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class NoteTypeLookup
{
    public int NoteTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public bool IsActive { get; set; }

    public bool IsSystemOnly { get; set; }

    public ICollection<WarningNote> WarningNotes { get; set; } = new List<WarningNote>();
}