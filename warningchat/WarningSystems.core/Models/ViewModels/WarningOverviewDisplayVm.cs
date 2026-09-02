namespace WarningSystems.Core.ViewModels;

public class WarningOverviewDisplayVm
{
    public string EmployeeName { get; set; } = "—";
    public string CategoryName { get; set; } = "—";
    public string DatesDisplay { get; set; } = "—";

    public List<(string Label, string Value)> DescriptionLines { get; set; } = [];

    public string SopDisplay { get; set; } = "—";

    public string PreviousOffencesDisplay { get; set; } = "—";
    public List<string> AttachmentNames { get; set; } = [];

    public string AdditionalInfoDisplay { get; set; } = "—";

    public int WarningId { get; set; }
}