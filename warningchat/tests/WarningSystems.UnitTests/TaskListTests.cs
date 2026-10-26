using Microsoft.Graph.Models;
using Moq;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.UnitTests;

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

    private static ManagerFixture Setup(string roles)
    {
        var f = new ManagerFixture();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var rows = new List<Warning> {
            new() { WarningId = 1, EmployeeId = "employee", CreatedBy = "actor", Status = "Draft", LegalExpiryDate = today.AddDays(-1) },
            new() { WarningId = 2, EmployeeId = "employee", CreatedBy = "actor", Status = "Hearing", LegalExpiryDate = today.AddDays(3) },
            new() { WarningId = 3, EmployeeId = "outside", CreatedBy = "actor", Status = "New", LegalExpiryDate = today.AddDays(4) },
            new() { WarningId = 4, EmployeeId = "outside", CreatedBy = "actor", Status = "In Progress" }
        };
        f.Data.Setup(x => x.GetAllWarningsForTaskListAsync()).ReturnsAsync(rows);
        f.Data.Setup(x => x.GetAllWarningsForReportingAsync()).ReturnsAsync(rows);
        f.Data.Setup(x => x.GetAllCategoriesAsync()).ReturnsAsync([]);
        f.Data.Setup(x => x.GetActiveIssueTypesAsync()).ReturnsAsync([]);
        f.Graph.Setup(x => x.GetUsersBelowMyLevelAsync()).ReturnsAsync([new User { Id = "employee", DisplayName = "Employee One", Department = "Sales" }]);
        f.Graph.Setup(x => x.GetMeAsync()).ReturnsAsync(new User { Id = "actor", DisplayName = "Manager" });
        f.Graph.Setup(x => x.GetUserAsync(It.IsAny<string>())).ReturnsAsync((string id) => new User { Id = id, DisplayName = id == "employee" ? "Employee One" : id });
        f.Roles.Setup(x => x.GetCurrentUserRolesAsync()).ReturnsAsync(roles.Split(','));
        return f;
    }
}
