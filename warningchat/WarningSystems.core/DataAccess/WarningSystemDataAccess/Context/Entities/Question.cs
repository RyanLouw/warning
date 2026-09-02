using WarningSystems.core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class Question : IAutomaticallyAuditedEntity
{
    public int QuestionId { get; set; }

    public string QuestionText { get; set; } = "";
    public string ControlType { get; set; } = "";

    public string? DefaultConfigJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }


    public ICollection<CategoryQuestion> CategoryLinks { get; set; } = new List<CategoryQuestion>();

    public ICollection<WarningAnswer> Answers { get; set; } = new List<WarningAnswer>();
}