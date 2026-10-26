using System.ComponentModel.DataAnnotations;
using WarningSystems.Core.Services;
using WarningSystems.core.Services;
using WarningSystems.Core.Manager;
using WarningSystems.Models.Validation;

namespace WarningSystems.UnitTests;

public class HelperTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("photo.png", "photo.png")]
    [InlineData("Evadance_photo.png", "photo.png")]
    [InlineData("EVADANCE_photo.png", "photo.png")]
    [InlineData("prefix_photo.png", "photo.png")]
    [InlineData("photo_123_456.png", "photo.png")]
    [InlineData("Evadance_photo_123_456.png", "photo.png")]
    [InlineData("photo", "photo")]
    [InlineData("a__b.txt", "b.txt")]
    public void Display_file_name(string? input, string expected) =>
        Assert.Equal(expected, FileNameHelper.GetDisplayFileName(input));

    [Theory]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    [InlineData("<p>Hello &amp; goodbye</p><p>Next</p>", "Hello & goodbye\nNext")]
    [InlineData("a<BR />b", "a\nb")]
    [InlineData("<ul><li>One</li><li>Two</li></ul>", "One\nTwo")]
    [InlineData("<h2>Title</h2>Text", "Title\nText")]
    [InlineData("Plain text", "Plain text")]
    [InlineData("&lt;script&gt;", "<script>")]
    public void Html_is_converted_to_plain_text(string? input, string expected) =>
        Assert.Equal(expected, TransgressionManager.HtmlToPlainText(input));

    [Fact]
    public void Recipients_are_trimmed_parsed_deduplicated_and_invalid_values_removed()
    {
        var result = EmailRecipientNormalizer.Normalize([
            " user@example.test ", "USER@example.test", "Name <other@example.test>",
            "not an email", "", "   ", null!]);
        Assert.Equal(2, result.Count);
        Assert.Contains("user@example.test", result, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("other@example.test", result);
    }

    [Fact]
    public void Null_recipients_are_empty() => Assert.Empty(EmailRecipientNormalizer.Normalize(null));

    [Theory]
    [InlineData(-10)]
    [InlineData(-1)]
    [InlineData(0)]
    public void Today_and_past_due_dates_are_rejected(int days)
    {
        var attribute = new FutureWeekdayAttribute();
        Assert.False(attribute.IsValid(DateOnly.FromDateTime(DateTime.Now).AddDays(days)));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Tuesday, true)]
    [InlineData(DayOfWeek.Wednesday, true)]
    [InlineData(DayOfWeek.Thursday, true)]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Saturday, false)]
    [InlineData(DayOfWeek.Sunday, false)]
    public void Future_dates_must_be_weekdays(DayOfWeek day, bool expected)
    {
        var date = DateOnly.FromDateTime(DateTime.Now).AddDays(7);
        while (date.DayOfWeek != day) date = date.AddDays(1);
        Assert.Equal(expected, new FutureWeekdayAttribute().IsValid(date));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026-01-01")]
    [InlineData(123)]
    public void Non_dates_are_rejected(object? value) =>
        Assert.False(new FutureWeekdayAttribute().IsValid(value));

    [Fact]
    public void Utc_is_converted_to_South_African_time_across_midnight()
    {
        var utc = new DateTime(2026, 1, 1, 23, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 1, 2, 1, 30, 0), utc.ToSast());
    }

    [Theory]
    [InlineData("IssueCreated")]
    [InlineData("LegalActionRequired")]
    public void Templates_encode_untrusted_values_and_cache_without_leaking_previous_values(string template)
    {
        var renderer = new EmbeddedEmailTemplateRenderer();
        var first = renderer.Render(template, new Dictionary<string, string?>
        {
            ["EmployeeDisplayName"] = "<script>&\"'", ["NoteText"] = "<script>&\"'"
        });
        Assert.DoesNotContain("<script>", first);
        Assert.Contains("&lt;script&gt;", first);
        var second = renderer.Render(template.ToLowerInvariant(), new Dictionary<string, string?>
        {
            ["EmployeeDisplayName"] = "Second User", ["NoteText"] = "Second User"
        });
        Assert.Contains("Second User", second);
        Assert.DoesNotContain("&lt;script&gt;", second);
    }

    [Fact]
    public void Missing_template_is_explicit() =>
        Assert.Throws<InvalidOperationException>(() => new EmbeddedEmailTemplateRenderer().Render("Missing", new Dictionary<string, string?>()));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Template_name_is_required(string? name) =>
        Assert.ThrowsAny<ArgumentException>(() => new EmbeddedEmailTemplateRenderer().Render(name!, new Dictionary<string, string?>()));

    [Fact]
    public void Template_values_are_required() =>
        Assert.Throws<ArgumentNullException>(() => new EmbeddedEmailTemplateRenderer().Render("IssueCreated", null!));
}
