using System.Text.Json;
using WarningSystems.Core.Services;
using WarningSystems.Core.ViewModels;

namespace WarningSystems.UnitTests;

public class OverviewMapperTests
{
    private readonly WarningOverviewMapperServise _mapper = new();

    [Fact]
    public void Empty_overview_has_readable_placeholders()
    {
        var result = _mapper.Build(new WarningWizardVm());
        Assert.Equal(0, result.WarningId);
        Assert.Equal("—", result.EmployeeName);
        Assert.Equal("—", result.DatesDisplay);
        Assert.Equal("—", result.SopDisplay);
        Assert.Equal("—", result.PreviousOffencesDisplay);
        Assert.Equal("—", result.AdditionalInfoDisplay);
        Assert.Empty(result.DescriptionLines);
        Assert.Empty(result.AttachmentNames);
    }

    [Theory]
    [InlineData("{\"dates\":[\"2026-01-01\",\"\",\"2026-01-02\"]}", "2026-01-01, 2026-01-02")]
    [InlineData("{\"dates\":[]}", "—")]
    [InlineData("{}", "—")]
    [InlineData("[]", "—")]
    [InlineData("", "—")]
    public void Dates_are_read_from_saved_json(string json, string expected)
    {
        var model = new WarningWizardVm { Questions = [new() { QuestionId = 1, AnswerJson = json }] };
        Assert.Equal(expected, _mapper.Build(model).DatesDisplay);
    }

    [Fact]
    public void Answer_text_takes_precedence_over_json()
    {
        var model = new WarningWizardVm { WarningId = 42, EmployeeId = " Employee ", Questions = [
            new() { QuestionId = 1, AnswerText = " saved dates ", AnswerJson = "invalid json" },
            new() { QuestionId = 2, QuestionText = "Description", AnswerText = " saved answer ", AnswerJson = "invalid json" }
        ] };
        var result = _mapper.Build(model);
        Assert.Equal(42, result.WarningId);
        Assert.Equal("Employee", result.EmployeeName);
        Assert.Equal("saved dates", result.DatesDisplay);
        Assert.Equal(("Description", "saved answer"), Assert.Single(result.DescriptionLines));
    }

    [Theory]
    [InlineData("{\"values\":[\"One\",\"\",\"Two\"]}", "One, Two")]
    [InlineData("{\"selected\":[\"One\",\"Two\"]}", "One, Two")]
    [InlineData("{\"text\":\"Typed text\"}", "Typed text")]
    [InlineData("{\"other\":true}", "{\"other\":true}")]
    [InlineData("{\"values\":[]}", "{\"values\":[]}")]
    [InlineData("[\"One\"]", "[\"One\"]")]
    public void Dynamic_answers_are_readable(string json, string expected)
    {
        var result = _mapper.Build(new WarningWizardVm { Questions = [new() { QuestionId = 4, QuestionText = "Answer", AnswerJson = json }] });
        Assert.Equal(expected, Assert.Single(result.DescriptionLines).Value);
    }

    [Fact]
    public void Invisible_and_empty_questions_are_omitted_and_visible_questions_are_ordered()
    {
        var result = _mapper.Build(new WarningWizardVm { Questions = [
            new() { QuestionId = 4, QuestionText = "Second", AnswerText = "B", SortOrder = 2 },
            new() { QuestionId = 5, QuestionText = "Hidden", AnswerText = "secret", IsQuestionActive = false },
            new() { QuestionId = 6, QuestionText = "Unlinked", AnswerText = "secret", IsCategoryLinkActive = false },
            new() { QuestionId = 7, QuestionText = "First", AnswerText = "A", SortOrder = 1 },
            new() { QuestionId = 8, QuestionText = "Empty" }
        ] });
        Assert.Equal(new[] { "First", "Second" }, result.DescriptionLines.Select(x => x.Label));
    }

    [Theory]
    [InlineData("{\"notApplicable\":true}", "SOP Not Applicable")]
    [InlineData("{\"sopDocumentName\":\" Safety SOP \"}", "Safety SOP")]
    [InlineData("{\"sopDocumentName\":\"\"}", "—")]
    [InlineData("{}", "—")]
    public void Sop_is_rendered(string json, string expected) =>
        Assert.Equal(expected, _mapper.Build(new WarningWizardVm { Questions = [new() { QuestionId = 3, AnswerJson = json }] }).SopDisplay);

    [Fact]
    public void Previous_offences_and_attachments_are_separated()
    {
        var result = _mapper.Build(new WarningWizardVm { EvidenceStored = ["PreviousOffences/old.pdf", "previousoffences/older.pdf", " files/photo.png ", " "], EvidenceNotes = " Notes " });
        Assert.Equal("old.pdf, older.pdf", result.PreviousOffencesDisplay);
        Assert.Equal("photo.png", Assert.Single(result.AttachmentNames));
        Assert.Equal("Notes", result.AdditionalInfoDisplay);
    }

    [Fact]
    public void Oversized_warning_id_does_not_silently_truncate() =>
        Assert.Throws<OverflowException>(() => _mapper.Build(new WarningWizardVm { WarningId = (long)int.MaxValue + 1 }));

    [Fact]
    public void Corrupt_saved_json_is_reported() =>
        Assert.ThrowsAny<JsonException>(() => _mapper.Build(new WarningWizardVm { Questions = [new() { QuestionId = 1, AnswerJson = "invalid" }] }));

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("", " ", false)]
    [InlineData("Text", null, true)]
    [InlineData(null, "{}", true)]
    public void Question_answer_state(string? text, string? json, bool expected) =>
        Assert.Equal(expected, new WarningQuestionVm { AnswerText = text, AnswerJson = json }.HasAnswer);

    [Theory]
    [InlineData(null, "default", "default")]
    [InlineData(" ", "default", "default")]
    [InlineData("override", "default", "override")]
    public void Category_config_overrides_question_default(string? category, string? fallback, string? expected) =>
        Assert.Equal(expected, new WarningQuestionVm { ConfigJson = category, DefaultConfigJson = fallback }.EffectiveConfigJson);

    [Theory]
    [InlineData(null, true)]
    [InlineData(0L, true)]
    [InlineData(-1L, true)]
    [InlineData(1L, false)]
    public void Wizard_identifies_new_issues(long? id, bool expected) =>
        Assert.Equal(expected, new WarningWizardVm { WarningId = id }.IsNew);
}
