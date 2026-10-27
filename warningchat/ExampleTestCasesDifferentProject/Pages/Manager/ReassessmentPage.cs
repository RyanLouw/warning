using CallQualityUITesting.Models;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace CallQualityUITesting.Pages.Manager;

public class ReassessmentPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public ReassessmentPage(
        IPage page,
        string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public ILocator Heading =>
        _page.GetByRole(
            AriaRole.Heading,
            new()
            {
                NameRegex = new Regex("Reassessing Assessment")
            });

    public ILocator QuestionRows =>
        _page.Locator("#reassessForm tbody tr");

    public ILocator SaveButton =>
        _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Save Reassessment" });

    public ILocator RecordingButton =>
        _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Play / Download Recording" });


    public async Task AnswerQuestionAsync(
        int rowIndex,
        ReassessmentAnswer answer)
    {
        var row = QuestionRows.Nth(rowIndex);

        var value = answer switch
        {
            ReassessmentAnswer.Yes => "yes",
            ReassessmentAnswer.No => "no",
            ReassessmentAnswer.NotApplicable => "na",
            _ => throw new ArgumentOutOfRangeException(nameof(answer))
        };

        await row
            .Locator($".answer-btn[data-value='{value}']")
            .ClickAsync();
    }


    public async Task AddNoteAsync(
        int rowIndex,
        string note)
    {
        var row = QuestionRows.Nth(rowIndex);

        await row
            .Locator(".reassess-note")
            .FillAsync(note);
    }
    public async Task SaveAsync()
    {
        string? dialogMessage = null;

        async void DialogHandler(object? sender, IDialog dialog)
        {
            dialogMessage = dialog.Message;
            await dialog.AcceptAsync();
        }

        _page.Dialog += DialogHandler;

        try
        {
            await SaveButton.ClickAsync();

            await _page.WaitForURLAsync(
                "**/Manager/Assessments**",
                new()
                {
                    Timeout = 30_000
                });
        }
        finally
        {
            _page.Dialog -= DialogHandler;
        }

        if (dialogMessage is null ||
            !dialogMessage.Contains("Reassessment saved"))
        {
            throw new InvalidOperationException(
                $"Unexpected save response: {dialogMessage}");
        }
    }

    public async Task CompleteAllQuestionsAsync()
    {
        var count = await QuestionRows.CountAsync();

        Assert.True(
            count > 0,
            "Expected at least one reassessment question.");

        for (var i = 0; i < count; i++)
        {
            var row = QuestionRows.Nth(i);

            var answer = i % 2 == 0
                ? ReassessmentAnswer.Yes
                : ReassessmentAnswer.No;

            await AnswerQuestionAsync(i, answer);

            await AddNoteAsync(
                i,
                $"Playwright reassessment question {i + 1}");

            await Assertions
                .Expect(
                    row.Locator(".answer-btn.active"))
                .ToHaveCountAsync(1);

            await Assertions
                .Expect(
                    row.Locator(".reassess-note"))
                .ToHaveValueAsync(
                    $"Playwright reassessment question {i + 1}");
        }
    }

    public async Task AnswerAllQuestionsAsync(
        ReassessmentAnswer answer)
    {
        var count = await QuestionRows.CountAsync();

        for (var i = 0; i < count; i++)
        {
            await AnswerQuestionAsync(i, answer);
        }
    }
}