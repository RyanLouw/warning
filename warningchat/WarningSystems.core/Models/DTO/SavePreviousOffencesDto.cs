using Microsoft.AspNetCore.Http;

namespace WarningSystems.Models.DTO;

public class SavePreviousOffencesDto
{
    public int WarningId { get; set; }

    public bool HasPreviousOffences { get; set; }

    public List<IFormFile> Files { get; set; } = [];
}
