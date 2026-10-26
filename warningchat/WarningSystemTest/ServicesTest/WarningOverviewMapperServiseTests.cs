using WarningSystems.Core.Services;
using WarningSystems.Core.ViewModels;

namespace WarningSystemTest.ServicesTest
{
    public class WarningOverviewMapperServiseTests
    {
        private static WarningWizardVm BaseModel(params WarningQuestionVm[] questions)
        {
            return new WarningWizardVm
            {
                WarningId = 123,
                EmployeeId = "  John Doe  ",
                Questions = new List<WarningQuestionVm>(questions)
            };
        }

        private static WarningQuestionVm Q(int id, string? text = null, string? json = null, string? questionText = null)
        {
            return new WarningQuestionVm
            {
                QuestionId = id,
                AnswerText = text,
                AnswerJson = json,
                QuestionText = questionText ?? $"Q{id}"
            };
        }

        [Fact]
        public void Build_Dates_UsesAnswerText_WhenPresent()
        {
            var sut = new WarningOverviewMapperServise();

            var model = BaseModel(
                Q(1, text: "  2026-02-01  ")
            );

            var vm = sut.Build(model);

            Assert.Equal("2026-02-01", vm.DatesDisplay);
        }

        [Fact]
        public void Build_Dates_UsesDatesArrayFromJson_WhenNoAnswerText()
        {
            var sut = new WarningOverviewMapperServise();

            var model = BaseModel(
                Q(1, text: "  ", json: @"{ ""dates"": [""2026-02-01"", ""2026-02-03""] }")
            );

            var vm = sut.Build(model);

            Assert.Equal("2026-02-01, 2026-02-03", vm.DatesDisplay);
        }

        [Fact]
        public void Build_Dates_IgnoresNullOrWhitespaceDates_InJsonArray()
        {
            var sut = new WarningOverviewMapperServise();

            var model = BaseModel(
                Q(1, text: null, json: @"{ ""dates"": [""2026-02-01"", null, ""  "", ""2026-02-03""] }")
            );

            var vm = sut.Build(model);

            Assert.Equal("2026-02-01, 2026-02-03", vm.DatesDisplay);
        }

        [Fact]
        public void Build_Dates_IsDash_WhenNoTextAndNoValidJsonDates()
        {
            var sut = new WarningOverviewMapperServise();

            var model = BaseModel(
                Q(1, text: null, json: @"{ ""dates"": [] }")
            );

            var vm = sut.Build(model);

            Assert.Equal("—", vm.DatesDisplay);
        }

        [Fact]
        public void Build_EmployeeAndCategory_AreTrimmed_AndDashWhenEmpty()
        {
            var sut = new WarningOverviewMapperServise();

            var model = new WarningWizardVm
            {
                WarningId = 5,
                EmployeeId = "   ",
                Questions = new List<WarningQuestionVm> { Q(1, text: "2026-02-01") }
            };

            var vm = sut.Build(model);

            Assert.Equal("—", vm.EmployeeName);
        }
    }
}
