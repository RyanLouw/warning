using Microsoft.Playwright;
using WarningSystemTest.Pages;
using WarningSystemTest.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.User;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "User")]
public sealed class DashboardTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Theory]
    [InlineData("UI Alice", "test-employee-alice")]
    [InlineData("UI Bob", "test-employee-bob")]
    [InlineData("UI Cara", "test-employee-cara")]
    public Task Task_search_matches_database_count(string name,string employee) => RunAsync($"task-{employee}", async page =>
    {
        await new HomePage(page).OpenAsync();
        await page.Locator("#taskListSearch").FillAsync(name);
        var expected=Convert.ToInt32(Assert.Single(await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning WHERE EmployeeId=@Employee AND IsDeleted=0", ("@Employee", employee)))["Total"]);
        var rows=page.Locator("#taskListTable tr.task-list-row:visible");
        await Expect(rows).ToHaveCountAsync(expected);
        foreach(var row in await rows.AllAsync()) await Expect(row.Locator("td").Nth(1)).ToHaveTextAsync(name);
        await page.Locator("#taskListSearch").FillAsync("no-such-person-98765");
        await Expect(rows).ToHaveCountAsync(0);
        await Expect(page.Locator("#taskListNoResults")).ToBeVisibleAsync();
    });

    [Fact]
    public Task Employee_table_counts_and_department_values_match_database() => RunAsync("employee-table",async page =>
    {
        await new HomePage(page).OpenAsync();
        await page.Locator("#employee-tab").ClickAsync();
        foreach(var (id,name,department) in new[] {
            ("test-employee-alice","UI Alice","Sales"),
            ("test-employee-bob","UI Bob","Support"),
            ("test-employee-cara","UI Cara","Sales")
        })
        {
            var total=Convert.ToInt32(Assert.Single(await Database.QueryAsync("SELECT COUNT(*) AS Total FROM ws.Warning WHERE EmployeeId=@Employee AND IsDeleted=0",("@Employee",id)))["Total"]);
            var row=page.Locator($"#employeeTable tr[data-employee-id='{id}']");
            await Expect(row.Locator("td").Nth(0)).ToHaveTextAsync(name);
            await Expect(row.Locator("td").Nth(1)).ToHaveTextAsync(department);
            await Expect(row.Locator("td").Nth(3)).ToHaveTextAsync("Test User");
            await Expect(row.Locator("td").Nth(4)).ToHaveTextAsync(total.ToString());
        }
        await page.Locator("#employeeSearch").FillAsync("Support");
        await Expect(page.Locator("#employeeTable .employee-row:visible")).ToHaveCountAsync(1);
        await Expect(page.Locator("#employeeTable .employee-row:visible td").First).ToHaveTextAsync("UI Bob");
    });
}
