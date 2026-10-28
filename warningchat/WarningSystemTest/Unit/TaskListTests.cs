using Microsoft.Graph.Models;
using FakeItEasy;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystemTest.Unit;

[Trait("Category", "Unit")]
public class TaskListTests
{
    [Theory]
    [InlineData("User", "1,2")]
    [InlineData("Legal", "2,3,4")]
    [InlineData("User,Legal", "1,2,3,4")]
    [InlineData("User,Chairperson", "1")]
    [InlineData("Legal,Chairperson", "3,4")]
    public async Task Task_and_report_rows_respect_roles_and_reporting_hierarchy(string roles, string expected)
    {
        var f = Setup(roles);
        var result = await f.Manager.BuildTaskListAsync();
        Assert.Equal(expected, string.Join(",", result.TaskList.Select(x => x.WarningId)));
        Assert.Equal(expected, string.Join(",", result.ReportRows.Select(x => x.WarningId)));
    }

    [Fact]
    public async Task Counts_and_employee_names_match_visible_rows()
    {
        var f = Setup("User,Legal");
        var result = await f.Manager.BuildTaskListAsync();
        Assert.Equal(1, result.DraftCount);
        Assert.Equal(1, result.New);
        Assert.Equal(1, result.InProgress);
        Assert.Equal(1, result.OverDue);
        Assert.Equal(1, result.Due);
        Assert.Equal("Employee One", result.TaskList[0].EmployeeDisplayName);
        var employee = Assert.Single(result.EmployeeRows);
        Assert.Equal(2, employee.NoWarnings);
        Assert.Equal("Sales", employee.Department);
    }

    [Fact]
    public async Task Creator_cannot_see_their_own_hidden_issue()
    {
        var fixture = Setup("User", [new Warning { WarningId = 42, EmployeeId = "employee",
            CreatedBy = "actor", HideFromTeamLead = true, Status = "New" }]);

        var result = await fixture.Manager.BuildTaskListAsync();

        Assert.Empty(result.TaskList);
    }

    [Fact]
    public async Task Hidden_issue_is_still_visible_to_another_team_lead_in_the_hierarchy()
    {
        // HideFromTeamLead applies to the creator, not every team lead.
        var fixture = Setup("User", [new Warning { WarningId = 42, EmployeeId = "employee",
            CreatedBy = "another-creator", HideFromTeamLead = true, Status = "New" }]);

        var result = await fixture.Manager.BuildTaskListAsync();

        Assert.Contains(result.TaskList, issue => issue.WarningId == 42);
    }

    [Fact]
    public async Task Chairperson_cannot_see_a_hearing_issue_type()
    {
        var fixture = Setup("Chairperson", [new Warning { WarningId = 42, EmployeeId = "employee",
            CreatedBy = "actor", Status = "Validated", IssueTypeId = 4,
            IssueType = new() { IssueTypeId = 4, IssueTypeName = "Hearing" } }]);

        var result = await fixture.Manager.BuildTaskListAsync();

        Assert.DoesNotContain(result.TaskList, issue => issue.WarningId == 42);
    }

    private static ManagerFixture Setup(string roles, List<Warning>? suppliedRows = null)
    {
        var f = new ManagerFixture();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var rows = suppliedRows ?? new List<Warning> {
            new() { WarningId = 1, EmployeeId = "employee", CreatedBy = "actor", Status = "Draft", LegalExpiryDate = today.AddDays(-1) },
            new() { WarningId = 2, EmployeeId = "employee", CreatedBy = "actor", Status = "Hearing", LegalExpiryDate = today.AddDays(3) },
            new() { WarningId = 3, EmployeeId = "outside", CreatedBy = "actor", Status = "New", LegalExpiryDate = today.AddDays(4) },
            new() { WarningId = 4, EmployeeId = "outside", CreatedBy = "actor", Status = "In Progress" }
        };
        A.CallTo(() => f.Data.GetAllWarningsForTaskListAsync()).Returns(rows);
        A.CallTo(() => f.Data.GetAllWarningsForReportingAsync()).Returns(rows);
        A.CallTo(() => f.Data.GetAllCategoriesAsync()).Returns([]);
        A.CallTo(() => f.Data.GetActiveIssueTypesAsync()).Returns([]);
        A.CallTo(() => f.Graph.GetUsersBelowMyLevelAsync()).Returns([new User { Id = "employee", DisplayName = "Employee One", Department = "Sales" }]);
        A.CallTo(() => f.Graph.GetMeAsync()).Returns(new User { Id = "actor", DisplayName = "Manager" });
        A.CallTo(() => f.Graph.GetUserAsync(A<string>._)).ReturnsLazily((string id) => new User { Id = id, DisplayName = id == "employee" ? "Employee One" : id });
        A.CallTo(() => f.Roles.GetCurrentUserRolesAsync()).Returns(roles.Split(','));
        return f;
    }
}
