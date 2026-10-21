using System.Text.Json;

namespace WarningSystems.Core.Services;

public static class DiscussionDateFormatter
{
    public static string Format(string? answerText, string? answerJson)
    {
        if (!string.IsNullOrWhiteSpace(answerText))
            return answerText.Trim();

        if (string.IsNullOrWhiteSpace(answerJson))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(answerJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("dates", out var dates) ||
                dates.ValueKind != JsonValueKind.Array)
                return string.Empty;

            return string.Join(", ", dates.EnumerateArray()
                .Where(date => date.ValueKind == JsonValueKind.String)
                .Select(date => date.GetString()?.Trim())
                .Where(date => !string.IsNullOrWhiteSpace(date)));
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}