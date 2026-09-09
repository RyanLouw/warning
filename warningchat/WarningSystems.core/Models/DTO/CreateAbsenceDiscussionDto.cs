using System.ComponentModel.DataAnnotations;

namespace WarningSystems.Models.DTO;

public class CreateAbsenceDiscussionDto
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int IssueSubTypeId { get; set; }

    [Required, MinLength(1)]
    public List<DateOnly> Dates { get; set; } = [];

    [Required]
    public string Description { get; set; } = string.Empty;
}
