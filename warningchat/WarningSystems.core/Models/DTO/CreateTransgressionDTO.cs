using System.ComponentModel.DataAnnotations;

namespace WarningSystems.Models.DTO;

public class CreateTransgressionDTO
{
    public long? WarningId { get; set; }

    [Required]
    public required string EmployeeId { get; set; }

    public int CategoryId { get; set; }

    public List<int> CategoryIds { get; set; } = [];
}