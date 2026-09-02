namespace WarningSystems.Models.DTO;

public class WarningDueDateHistory
{
    public long Id { get; set; }
    public long WarningId { get; set; }

    public DateOnly OldDueDate { get; set; }
    public DateOnly NewDueDate { get; set; }

    public string Reason { get; set; } = "";
    public string ChangedBy { get; set; } = "";
    public DateTime ChangedOnUtc { get; set; }
}