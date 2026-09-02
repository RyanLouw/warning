namespace WarningSystems.Core.Services;

public static class FileNameHelper
{
    public static string GetDisplayFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var ext = System.IO.Path.GetExtension(fileName);
        var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);

        if (nameWithoutExt.StartsWith("Evadance_", StringComparison.OrdinalIgnoreCase))
            nameWithoutExt = nameWithoutExt.Substring("Evadance_".Length);

        var parts = nameWithoutExt.Split('_', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 3)
        {
            return parts[0] + ext;
        }

        if (parts.Length == 2)
        {
            return parts[1] + ext;
        }

        return nameWithoutExt + ext;
    }
}