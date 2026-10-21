namespace WarningSystems.Core.ViewModels;

public class LegalOverviewVm
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public List<TransgretionsVM> Issues { get; init; } = [];

    public static LegalOverviewVm Create(
    IEnumerable<TransgretionsVM> rows,
    DateTime now)
    {
        var issues = rows
            .Where(row => IsIncludedStatus(row.Status))
            .OrderByDescending(row => row.CreatedOn)
            .ThenByDescending(row => row.WarningId)
            .ToList();

        return new LegalOverviewVm
        {
            From = issues.Count > 0
                ? issues.Min(row => row.CreatedOn)
                : now,
            To = now,
            Issues = issues
        };
    }

    private static bool IsIncludedStatus(string? status)
    {
        var normalized = status?.Trim();

        return string.Equals(
                   normalized,
                   "Pending",
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   normalized,
                   "More Information Requested",
                   StringComparison.OrdinalIgnoreCase);
    }
}