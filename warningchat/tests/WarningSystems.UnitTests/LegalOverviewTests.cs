using WarningSystems.Core.ViewModels;

namespace WarningSystems.UnitTests;

public class LegalOverviewTests
{
    [Theory]
    [InlineData("Pending", true)]
    [InlineData(" pending ", true)]
    [InlineData("More Information Requested", true)]
    [InlineData("MORE INFORMATION REQUESTED", true)]
    [InlineData("Draft", false)]
    [InlineData("New", false)]
    [InlineData("Completed", false)]
    [InlineData(null, false)]
    public void Includes_only_pending_and_requested_information(string? status, bool included)
    {
        var now = new DateTime(2026, 10, 26);
        var result = LegalOverviewVm.Create([new() { EmployeeId = "employee", CreatedBy = "actor", EmployeeDisplayName = "Employee", WarningId = 1, Status = status!, CreatedOn = now.AddYears(-1) }], now);
        Assert.Equal(included ? 1 : 0, result.Issues.Count);
        Assert.Equal(included ? now.AddYears(-1) : now, result.From);
        Assert.Equal(now, result.To);
    }

    [Fact]
    public void Sorts_newest_first_and_uses_id_to_break_ties()
    {
        var now = new DateTime(2026, 10, 26);
        var result = LegalOverviewVm.Create([
            new() { EmployeeId = "employee", CreatedBy = "actor", EmployeeDisplayName = "Employee", WarningId = 1, Status = "Pending", CreatedOn = now.AddDays(-10) },
            new() { EmployeeId = "employee", CreatedBy = "actor", EmployeeDisplayName = "Employee", WarningId = 2, Status = "Pending", CreatedOn = now },
            new() { EmployeeId = "employee", CreatedBy = "actor", EmployeeDisplayName = "Employee", WarningId = 3, Status = "Pending", CreatedOn = now }
        ], now);
        Assert.Equal(new long[] { 3, 2, 1 }, result.Issues.Select(i => i.WarningId));
        Assert.Equal(now.AddDays(-10), result.From);
    }
}
