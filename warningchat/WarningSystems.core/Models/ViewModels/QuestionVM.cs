using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.Core.ViewModels;

public class QuestionVM
{
    public QuestionVM()
    {
    }

    public QuestionVM(
        Question entity,
        string? createdByDisplayName = null,
        string? updatedByDisplayName = null)
    {
        QuestionId = entity.QuestionId;
        QuestionText = entity.QuestionText;
        ControlType = entity.ControlType;
        DefaultConfigJson = entity.DefaultConfigJson;
        IsActive = entity.IsActive;
        CreatedOn = entity.CreatedOn;

        CreatedBy = createdByDisplayName
                    ?? entity.CreatedBy;

        UpdatedOn = entity.UpdatedOn;

        UpdatedBy = updatedByDisplayName
                    ?? entity.UpdatedBy;
    }

    public int QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public string ControlType { get; set; } = string.Empty;

    public string? DefaultConfigJson { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOn { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}