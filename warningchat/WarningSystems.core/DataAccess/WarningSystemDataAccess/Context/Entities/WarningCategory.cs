namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class WarningCategory
{
    public long WarningId { get; set; }
    public int CategoryId { get; set; }

    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = "";

    public Warning Warning { get; set; } = null!;
    public TransgressionCategory Category { get; set; } = null!;
}