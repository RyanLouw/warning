using Microsoft.AspNetCore.Http;

namespace WarningSystems.Core.ViewModels;

public class AddTeamLeadEvidenceVm
{
    public long WarningId { get; set; }

    public string? NoteText { get; set; }
    public string? CurrentUser { get; set; }

    public List<IFormFile> Files { get; set; } = new();
}
