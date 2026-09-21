using Microsoft.AspNetCore.Http;

namespace WarningSystems.Core.ViewModels;

public class RequestTeamLeadInformationVm
{
    public long WarningId { get; set; }

    public string Message { get; set; } = string.Empty;

    public IFormFile? File { get; set; }
}