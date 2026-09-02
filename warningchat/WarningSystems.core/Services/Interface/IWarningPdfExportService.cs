using WarningSystems.Core.ViewModels;

namespace WarningSystems.Services.Interface;

public interface IWarningPdfExportService
{
    Task<byte[]> ExportOverviewPdfAsync(WarningWizardVm model);
}