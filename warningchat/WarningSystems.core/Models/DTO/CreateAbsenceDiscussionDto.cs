using System.ComponentModel.DataAnnotations;

namespace WarningSystems.Models.DTO;

public class CreateAbsenceDiscussionDto
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string SubType { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<DateOnly> Dates { get; set; } = [];

    [Required]
    public string Description { get; set; } = string.Empty;
}
