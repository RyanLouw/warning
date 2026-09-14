namespace WarningSystems.Models.DTO;

using System.ComponentModel.DataAnnotations;
using WarningSystems.Models.Validation;

public class UpdateDueDateDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Missing WarningId.")]
    public long WarningId { get; set; }

    [Required(ErrorMessage = "Due date is required.")]
    [FutureWeekday(ErrorMessage = "Due date must be a future weekday.")]
    public DateOnly DueDate { get; set; }

    [Required(ErrorMessage = "Reason is required.")]
    [MinLength(3, ErrorMessage = "Reason is too short.")]
    public string Reason { get; set; } = "";
}

public class UpdateLegalIssueDetailsDto
{
    [Range(1, long.MaxValue)]
    public long WarningId { get; set; }

    [Required]
    public string UpdateType { get; set; } = string.Empty;

    public List<int> CategoryIds { get; set; } = [];

    public List<DateOnly> Dates { get; set; } = [];
}
