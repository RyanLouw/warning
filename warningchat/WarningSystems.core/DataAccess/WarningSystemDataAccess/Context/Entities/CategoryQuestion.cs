using WarningSystems.core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class CategoryQuestion : IAutomaticallyAuditedEntity
{
    public int CategoryId { get; set; }
    public int QuestionId { get; set; }

    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }

    public string? ConfigJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }

    public TransgressionCategory? Category { get; set; }
    public Question? Question { get; set; }
}