using System.ComponentModel.DataAnnotations;

namespace WarningSystems.core.Models.DTO;

public class UpdateLegalIssueDetailsDto
{
    [Range(1, long.MaxValue)]
    public long WarningId { get; set; }

    [Required]
    public string UpdateType { get; set; } = string.Empty;

    public List<int> CategoryIds { get; set; } = [];

    public List<DateOnly> Dates { get; set; } = [];
}
