using System.Text.Json;
using WarningSystems.Core.ViewModels;

namespace WarningSystems.Core.Services;

public class WarningOverviewMapperServise
{
    public WarningOverviewDisplayVm Build(WarningWizardVm model)
    {
        var questions = model.Questions ?? new List<WarningQuestionVm>();

        WarningQuestionVm? Q(int id) => questions.FirstOrDefault(x => x.QuestionId == id);

        string ValOrDash(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();

        string GetDatesDisplayFromQuestion(int questionId)
        {
            var q = Q(questionId);
            if (q == null) return "";

            var text = (q.AnswerText ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(text)) return text;

            var json = (q.AnswerJson ?? "").Trim();
            if (string.IsNullOrWhiteSpace(json)) return "";

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("dates", out var datesEl) &&
                datesEl.ValueKind == JsonValueKind.Array)
            {
                var dates = datesEl.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                return dates.Count > 0 ? string.Join(", ", dates!) : "";
            }

            return "";
        }

        string GetPrettyAnswer(WarningQuestionVm q)
        {
            if (!string.IsNullOrWhiteSpace(q.AnswerText))
                return q.AnswerText!.Trim();

            if (!string.IsNullOrWhiteSpace(q.AnswerJson))
            {
                var json = q.AnswerJson!.Trim();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("values", out var valuesEl) &&
                        valuesEl.ValueKind == JsonValueKind.Array)
                    {
                        var vals = valuesEl.EnumerateArray()
                            .Select(x => x.GetString())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToList();
                        if (vals.Count > 0) return string.Join(", ", vals!);
                    }

                    if (doc.RootElement.TryGetProperty("selected", out var selEl) &&
                        selEl.ValueKind == JsonValueKind.Array)
                    {
                        var vals = selEl.EnumerateArray()
                            .Select(x => x.GetString())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToList();
                        if (vals.Count > 0) return string.Join(", ", vals!);
                    }

                    if (doc.RootElement.TryGetProperty("text", out var textEl) &&
                        textEl.ValueKind == JsonValueKind.String)
                    {
                        var t = textEl.GetString();
                        if (!string.IsNullOrWhiteSpace(t)) return t!;
                    }
                }

                return json;
            }

            return "";
        }

        var warningIdInt = model.WarningId.HasValue
            ? checked((int)model.WarningId.Value)
            : 0;

        var vm = new WarningOverviewDisplayVm
        {
            WarningId = warningIdInt,
            EmployeeName = ValOrDash(model.EmployeeId),
            DatesDisplay = ValOrDash(GetDatesDisplayFromQuestion(1))
        };

        // Description
        var q2 = Q(2);

        var descriptionOtherQs = questions
            .Where(q => q.QuestionId != 1 && q.QuestionId != 2 && q.QuestionId != 3)
            .Where(q => q.IsVisible)
            .OrderBy(q => q.DisplayOrder)
            .ToList();

        if (q2 != null)
        {
            var mainDesc = GetPrettyAnswer(q2);
            if (!string.IsNullOrWhiteSpace(mainDesc))
                vm.DescriptionLines.Add((q2.QuestionText ?? "Description", mainDesc));
        }

        foreach (var q in descriptionOtherQs)
        {
            var ans = GetPrettyAnswer(q);
            if (!string.IsNullOrWhiteSpace(ans))
                vm.DescriptionLines.Add((q.QuestionText, ans));
        }

        // SOP
        var sopQ = Q(3);
        if (sopQ != null && !string.IsNullOrWhiteSpace(sopQ.AnswerJson))
        {
            using var doc = JsonDocument.Parse(sopQ.AnswerJson);
            if (doc.RootElement.TryGetProperty("notApplicable", out var na) && na.GetBoolean())
                vm.SopDisplay = "SOP Not Applicable";
            else if (doc.RootElement.TryGetProperty("sopDocumentName", out var name))
                vm.SopDisplay = ValOrDash(name.GetString());
            else
                vm.SopDisplay = "—";
        }
        else
        {
            vm.SopDisplay = "—";
        }

        // Files
        var allFiles = (model.EvidenceStored ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        var previousOffenceFiles = allFiles
            .Where(f => f.StartsWith("PreviousOffences", StringComparison.OrdinalIgnoreCase))
            .ToList();

        vm.PreviousOffencesDisplay =
            previousOffenceFiles.Count == 0
                ? "—"
                : string.Join(", ", previousOffenceFiles.Select(f => f.Split('/').Last()));

        vm.AttachmentNames = allFiles
            .Where(f => !f.StartsWith("PreviousOffences", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Split('/').Last())
            .ToList();

        // Additional info
        var add = (model.EvidenceNotes ?? "").Trim();
        vm.AdditionalInfoDisplay = string.IsNullOrWhiteSpace(add) ? "—" : add;

        return vm;
    }
}