using WarningSystems.core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class TransgressionCategory: IAutomaticallyAuditedEntity
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }

    public ICollection<CategoryQuestion> CategoryQuestions { get; set; } = new List<CategoryQuestion>();

    public ICollection<Warning> Warnings { get; set; } = new List<Warning>();
}