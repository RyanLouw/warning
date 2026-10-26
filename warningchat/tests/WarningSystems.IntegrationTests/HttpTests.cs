using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarningSystems.Core.Manager;
using WarningSystems.Testing;

namespace WarningSystems.IntegrationTests;

public class HttpTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Theory]
    [InlineData("Anonymous", "/Home/Index", HttpStatusCode.Unauthorized)]
    [InlineData("Anonymous", "/Admin/Index", HttpStatusCode.Unauthorized)]
    [InlineData("User", "/Admin/Index", HttpStatusCode.Forbidden)]
    [InlineData("Legal", "/Admin/Index", HttpStatusCode.Forbidden)]
    [InlineData("Admin", "/Admin/Index", HttpStatusCode.OK)]
    [InlineData("User", "/Home/Index", HttpStatusCode.OK)]
    [InlineData("Legal", "/Home/Index", HttpStatusCode.OK)]
    [InlineData("User,Legal", "/Home/Index", HttpStatusCode.OK)]
    [InlineData("User", "/Transgression/Index", HttpStatusCode.OK)]
    [InlineData("User", "/Transgression/Index/1", HttpStatusCode.OK)]
    [InlineData("User", "/Transgression/TeamLeadWarning/2", HttpStatusCode.OK)]
    [InlineData("Legal", "/Legal/Index/2", HttpStatusCode.OK)]
    [InlineData("Legal", "/Legal/Index/999999", HttpStatusCode.NotFound)]
    [InlineData("User", "/Home/EmployeeDashboard?employeeId=alice", HttpStatusCode.OK)]
    [InlineData("User", "/Home/AccessDenied", HttpStatusCode.OK)]
    [InlineData("User", "/Home/Error", HttpStatusCode.OK)]
    public async Task Routes_enforce_real_authorization(string roles, string route, HttpStatusCode expected)
    {
        using var client = fixture.App.ClientFor(roles);
        var response = await client.GetAsync(route);
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Test Manager", body);
            Assert.DoesNotContain("login.microsoftonline.com", response.RequestMessage!.RequestUri!.Host);
        }
    }

    [Fact]
    public async Task Pdf_export_is_a_valid_download_for_saved_warning()
    {
        using var client = fixture.App.ClientFor("User");
        var response = await client.GetAsync("/Transgression/ExportOverviewPdf?warningId=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes[..8]));
        Assert.True(bytes.Length > 500);
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected_and_does_not_write()
    {
        using var client = fixture.App.ClientFor("User");
        await using var before = fixture.App.OpenDatabase();
        var count = await before.Warnings.CountAsync();
        var response = await client.PostAsJsonAsync("/Home/CreateAbsenceDiscussion", new {
            EmployeeId = "alice", IssueSubTypeId = 2, Dates = new[] { "2026-01-01" }, Description = "must not be saved"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var after = fixture.App.OpenDatabase();
        Assert.Equal(count, await after.Warnings.CountAsync());
    }

    [Theory]
    [InlineData("User", "Draft", "Transgression")]
    [InlineData("User", "Completed", "Transgression")]
    [InlineData("Legal", "Completed", "Legal")]
    public async Task Email_redirects_work_through_http(string roles, string status, string controller)
    {
        using var client = fixture.App.ClientFor(roles);
        var response = await client.GetAsync($"/Home/Redirect?warningId=2&status={status}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith($"/{controller}/", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Manager_report_ids_match_independent_database_query()
    {
        // Establish the host, then create a request scope with the synthetic combined-role identity.
        using var client = fixture.App.ClientFor("User,Legal");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Home/Index")).StatusCode);
        await using var db = fixture.App.OpenDatabase();
        var expected = await db.Warnings.AsNoTracking().Where(w => !w.IsDeleted).OrderBy(w => w.WarningId).Select(w => w.WarningId).ToArrayAsync();
        var html = await client.GetStringAsync("/Home/Index");
        var actual = System.Text.RegularExpressions.Regex.Matches(html, "data-warning-id=\"(\\d+)\"")
            .Select(m => long.Parse(m.Groups[1].Value)).Order().ToArray();
        Assert.Equal(expected, actual);
        Assert.DoesNotContain(14L, actual);
    }
}
