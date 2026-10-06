namespace WarningSystems.Core.ViewModels;

public class LegalOverviewVm
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public List<TransgretionsVM> Issues { get; init; } = [];

    public static LegalOverviewVm Create(IEnumerable<TransgretionsVM> rows, DateTime now)
    {
        var from = now.AddDays(-30);

        return new LegalOverviewVm
        {
            From = from,
            To = now,
            Issues = rows
                .Where(row => row.CreatedOn >= from && row.CreatedOn <= now)
                .Where(row => !IsExcludedStatus(row.Status))
                .OrderByDescending(row => row.CreatedOn)
                .ThenByDescending(row => row.WarningId)
                .ToList()
        };
    }

    private static bool IsExcludedStatus(string? status)
    {
        var normalized = status?.Trim();
        return string.Equals(normalized, "Draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Invalid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Validated", StringComparison.OrdinalIgnoreCase);
    }
}
