using System.Text.Json;
using Microsoft.Playwright;
using WarningSystemTest.Fixtures;
using WarningSystemTest.Models;
using WarningSystemTest.SystemUnderTest;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests;

[Collection("Warning browser")]
public sealed class LoginTests(BrowserFixture browser)
{
    [Fact]
    public async Task Cannot_authenticate_without_the_per_run_test_key()
    {
        await using var context = await browser.CreateContextAsync(null);
        var response = await context.APIRequest.GetAsync("/__testing/session");
        Assert.Equal(401, response.Status);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Legal")]
    [InlineData("Admin")]
    [InlineData("Chairperson")]
    public async Task Can_open_warning_system_as_each_test_user(string role)
    {
        var user = TestUsers.ForRole(role);
        await using var context = await browser.CreateContextAsync(user);
        var page = await context.NewPageAsync();
        try
        {
            var response = await page.GotoAsync("/", new() { Timeout = (float)WarningSystemSut.DefaultTimeout.TotalMilliseconds });
            Assert.NotNull(response);
            Assert.Equal(200, response.Status);
            Assert.Equal(new Uri(browser.BaseUrl).Authority, new Uri(page.Url).Authority);
            await Expect(page.Locator("nav")).ToBeVisibleAsync();

            // The endpoint exists only in the test build. It reports the actual
            // server-side identity; successful navigation alone is insufficient.
            var session = await context.APIRequest.GetAsync("/__testing/session");
            Assert.Equal(200, session.Status);
            using var document = JsonDocument.Parse(await session.TextAsync());
            var identity = document.RootElement;
            Assert.True(identity.GetProperty("authenticated").GetBoolean());
            Assert.Equal(user.ObjectId, identity.GetProperty("objectId").GetString());
            Assert.Equal(user.DisplayName, identity.GetProperty("name").GetString());
            Assert.Equal(new[] { user.Role }, identity.GetProperty("roles").EnumerateArray().Select(item => item.GetString()));
        }
        catch
        {
            var directory = Path.GetFullPath(Path.Combine("TestResults", "Login", $"{role}-{Guid.NewGuid():N}"));
            Directory.CreateDirectory(directory);
            await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "failure.png"), FullPage = true });
            await context.Tracing.StopAsync(new() { Path = Path.Combine(directory, "trace.zip") });
            throw;
        }
    }
}
