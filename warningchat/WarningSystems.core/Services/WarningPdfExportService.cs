using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using WarningSystems.Core.ViewModels;
using WarningSystems.Services.Interface;

namespace WarningSystems.Core.Services;

public class WarningPdfExportService : IWarningPdfExportService
{
    private readonly WarningOverviewMapperServise _mapper;

    public WarningPdfExportService(
        WarningOverviewMapperServise mapper)
    {
        _mapper = mapper;

        QuestPDF.Settings.License =
            LicenseType.Community;
    }

    public Task<byte[]> ExportOverviewPdfAsync(
        WarningWizardVm model)
    {
        var vm = _mapper.Build(model);

        var selectedCategoryNames =
            (model.Categories ?? new List<CategoryVM>())
                .Where(category =>
                    model.CategoryIds != null &&
                    model.CategoryIds.Contains(
                        category.CategoryId))
                .Select(category => category.Name)
                .Where(name =>
                    !string.IsNullOrWhiteSpace(name))
                .ToList();

        var categoryDisplay =
            selectedCategoryNames.Count > 0
                ? string.Join(
                    ", ",
                    selectedCategoryNames)
                : "—";

        var sopDisplay = GetSopDisplay(model);

        string CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "—";

            var cleaned = Regex.Replace(
                value,
                @"<\s*br\s*/?\s*>|</\s*p\s*>|</\s*div\s*>|</\s*li\s*>",
                Environment.NewLine,
                RegexOptions.IgnoreCase);

            cleaned = Regex.Replace(
                cleaned,
                @"<[^>]*>",
                "",
                RegexOptions.IgnoreCase);

            cleaned =
                WebUtility.HtmlDecode(cleaned);

            var lines = cleaned
                .Replace('\u00A0', ' ')
                .Split(
                    new[]
                    {
                        "\r\n",
                        "\r",
                        "\n"
                    },
                    StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line =>
                    !string.IsNullOrWhiteSpace(line));

            return string.Join(
                Environment.NewLine,
                lines);
        }

        List<string> GetDates(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            return value
                .Split(
                    new[]
                    {
                        ",",
                        ";",
                        "\r\n",
                        "\r",
                        "\n"
                    },
                    StringSplitOptions
                        .RemoveEmptyEntries)
                .Select(date => date.Trim())
                .Where(date =>
                    !string.IsNullOrWhiteSpace(date))
                .ToList();
        }


        var dates =
            GetDates(vm.DatesDisplay);


        var bytes =
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(
                        PageSizes.A4.Landscape());

                    page.Margin(18);

                    page.DefaultTextStyle(
                        style =>
                            style.FontSize(9));


                    page.Content()
                        .Column(column =>
                        {
                            column.Item()
                                .Row(row =>
                                {
                                    /*
                                     * 1. Issue
                                     */
                                    row.RelativeItem()
                                        .Padding(6)
                                        .Element(container =>
                                            Card(
                                                container,
                                                "1. Issue",
                                                body =>
                                                {
                                                    body.Item()
                                                        .PaddingBottom(4)
                                                        .Column(
                                                            issueColumn =>
                                                            {
                                                                issueColumn
                                                                    .Item()
                                                                    .Text(
                                                                        "Employee Name:")
                                                                    .FontColor(
                                                                        Colors
                                                                            .Grey
                                                                            .Darken2);

                                                                issueColumn
                                                                    .Item()
                                                                    .Text(
                                                                        CleanText(
                                                                            model.EmployeeName))
                                                                    .SemiBold();
                                                            });

                                                    body.Item()
                                                        .PaddingTop(4)
                                                        .Column(
                                                            issueColumn =>
                                                            {
                                                                issueColumn
                                                                    .Item()
                                                                    .Text(
                                                                        "Issue Type:")
                                                                    .FontColor(
                                                                        Colors
                                                                            .Grey
                                                                            .Darken2);

                                                                issueColumn
                                                                    .Item()
                                                                    .Text(
                                                                        categoryDisplay)
                                                                    .SemiBold();
                                                            });
                                                }));


                                    /*
                                     * 2. Dates
                                     */
                                    row.RelativeItem()
                                        .Padding(6)
                                        .Element(container =>
                                            Card(
                                                container,
                                                "2. Dates",
                                                body =>
                                                {
                                                    if (dates.Count == 0)
                                                    {
                                                        body.Item()
                                                            .Text("—");

                                                        return;
                                                    }

                                                    body.Item()
                                                        .Text("Date:")
                                                        .FontColor(
                                                            Colors
                                                                .Grey
                                                                .Darken2);

                                                    foreach (
                                                        var date in dates)
                                                    {
                                                        body.Item()
                                                            .PaddingTop(2)
                                                            .Row(
                                                                dateRow =>
                                                                {
                                                                    dateRow
                                                                        .ConstantItem(
                                                                            10)
                                                                        .Text("•")
                                                                        .FontColor(
                                                                            Colors
                                                                                .Grey
                                                                                .Darken1);

                                                                    dateRow
                                                                        .RelativeItem()
                                                                        .Text(date)
                                                                        .SemiBold();
                                                                });
                                                    }
                                                }));
                                });


                            /*
                             * 3. Description
                             * Full width.
                             */
                            column.Item()
                                .Padding(6)
                                .Element(container =>
                                    Card(
                                        container,
                                        "3. Description",
                                        body =>
                                        {
                                            if (
                                                vm.DescriptionLines.Count ==
                                                0)
                                            {
                                                body.Item()
                                                    .Text("—");

                                                return;
                                            }

                                            foreach (
                                                var item in
                                                vm.DescriptionLines)
                                            {
                                                var label =
                                                    CleanText(
                                                        item.Label);

                                                var value =
                                                    CleanText(
                                                        item.Value);

                                                body.Item()
                                                    .PaddingBottom(8)
                                                    .Column(
                                                        description =>
                                                        {
                                                            description
                                                                .Item()
                                                                .Text(label)
                                                                .FontColor(
                                                                    Colors
                                                                        .Grey
                                                                        .Darken2);

                                                            description
                                                                .Item()
                                                                .PaddingTop(2)
                                                                .Text(value)
                                                                .SemiBold();
                                                        });
                                            }
                                        }));

                            column.Item()
                                .Row(row =>
                                {
                                    /*
                                     * 4. SOP Compliance
                                     */
                                    row.RelativeItem()
                                        .Padding(6)
                                        .Element(container =>
                                            Card(
                                                container,
                                                "4. SOP Compliance",
                                                body =>
                                                {
                                                    body.Item()
                                                        .Text(
                                                            "SOP Name:")
                                                        .FontColor(
                                                            Colors
                                                                .Grey
                                                                .Darken2);

                                                    body.Item()
                                                        .PaddingTop(2)
                                                        .Text(
                                                            CleanText(
                                                                sopDisplay))
                                                        .SemiBold();
                                                }));


                                    /*
                                     * 5. Attachments
                                     */
                                    row.RelativeItem()
                                        .Padding(6)
                                        .Element(container =>
                                            Card(
                                                container,
                                                "5. Attachments",
                                                body =>
                                                {
                                                    if (
                                                        vm.AttachmentNames
                                                            .Count == 0)
                                                    {
                                                        body.Item()
                                                            .Text("—");

                                                        return;
                                                    }

                                                    foreach (
                                                        var fileName in
                                                        vm.AttachmentNames)
                                                    {
                                                        body.Item()
                                                            .PaddingBottom(2)
                                                            .Row(
                                                                fileRow =>
                                                                {
                                                                    fileRow
                                                                        .ConstantItem(
                                                                            10)
                                                                        .Text("•")
                                                                        .FontColor(
                                                                            Colors
                                                                                .Grey
                                                                                .Darken1);

                                                                    fileRow
                                                                        .RelativeItem()
                                                                        .Text(
                                                                            CleanText(
                                                                                fileName))
                                                                        .SemiBold();
                                                                });
                                                    }
                                                }));
                                });
                        });


                    page.Footer()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span(
                                    "Generated: ")
                                .FontColor(
                                    Colors
                                        .Grey
                                        .Darken1);

                            text.Span(
                                    DateTime.UtcNow
                                        .AddHours(2)
                                        .ToString(
                                            "yyyy-MM-dd HH:mm"))
                                .SemiBold();
                        });
                });
            })
            .GeneratePdf();

        return Task.FromResult(bytes);
    }


    private static string GetSopDisplay(
        WarningWizardVm model)
    {
        var sopQuestion =
            model.Questions?
                .FirstOrDefault(question =>
                    question.QuestionId == 3);

        if (string.IsNullOrWhiteSpace(
                sopQuestion?.AnswerJson))
        {
            return "—";
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    sopQuestion.AnswerJson);

            var root =
                document.RootElement;

            if (root.TryGetProperty(
                    "notApplicable",
                    out var notApplicable))
            {
                var isNotApplicable =
                    notApplicable.ValueKind ==
                    JsonValueKind.True;

                if (isNotApplicable)
                {
                    return "SOP Not Applicable";
                }
            }


            if (root.TryGetProperty(
                    "sopDocumentId",
                    out var sopIdElement) &&
                sopIdElement.TryGetInt32(
                    out var sopDocumentId))
            {
                var selectedSop =
                    model.SOPs?
                        .FirstOrDefault(sop =>
                            sop.SOPDocumentId ==
                            sopDocumentId);

                if (selectedSop != null &&
                    !string.IsNullOrWhiteSpace(
                        selectedSop.DocumentName))
                {
                    return selectedSop.DocumentName;
                }
            }

            if (root.TryGetProperty(
                    "sopDocumentName",
                    out var sopNameElement) &&
                sopNameElement.ValueKind ==
                    JsonValueKind.String)
            {
                var sopName =
                    sopNameElement.GetString();

                if (!string.IsNullOrWhiteSpace(
                        sopName))
                {
                    return sopName;
                }
            }
        }
        catch (JsonException)
        {
            return "—";
        }

        return "—";
    }


    private static void Card(
        IContainer container,
        string title,
        Action<ColumnDescriptor> bodyContent)
    {
        var headerBg =
            Colors.Orange.Lighten5;

        var headerText =
            Colors.Orange.Darken2;

        container
            .Border(1)
            .BorderColor(
                Colors.Grey.Lighten2)
            .Background(
                Colors.White)
            .CornerRadius(8)
            .Column(column =>
            {
                column.Item()
                    .Background(
                        headerBg)
                    .PaddingVertical(8)
                    .PaddingHorizontal(10)
                    .Text(title)
                    .SemiBold()
                    .FontSize(10)
                    .FontColor(
                        headerText);

                column.Item()
                    .Padding(10)
                    .Column(body =>
                    {
                        body.Spacing(6);

                        bodyContent(body);
                    });
            });
    }
}