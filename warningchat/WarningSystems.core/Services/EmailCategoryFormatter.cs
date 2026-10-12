namespace WarningSystems.Core.Services;

public static class EmailCategoryFormatter
{
    public static string Format(IEnumerable<string?> selectedNames, string? primaryName, int primaryId)
    {
        var names = selectedNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return names.Count > 0
            ? string.Join(", ", names)
            : !string.IsNullOrWhiteSpace(primaryName)
                ? primaryName.Trim()
                : primaryId.ToString();
    }
}
