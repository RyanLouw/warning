using System.Security.Cryptography;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WarningSystemTest.Models;
using WarningSystemTest.Helpers;
using WarningSystemTest.SystemUnderTest;

[assembly: AssemblyFixture(typeof(WarningSystemSut))]

namespace WarningSystemTest.SystemUnderTest;

public sealed class WarningSystemSut : IAsyncLifetime
{
    private DistributedApplication? _application;
    private HttpClient? _client;
    private string? _connectionString;
    private readonly string _key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static TimeSpan DefaultTimeout => TimeSpan.FromMinutes(5);
    public string BaseUrl => _client?.BaseAddress?.ToString() ?? throw new InvalidOperationException("Aspire has not started the warning system.");

    public async ValueTask InitializeAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(DefaultTimeout);
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.WarningSystemsAppHost>(
            ["Testing:Enabled=true", "DcpPublisher:RandomizePorts=true", $"Testing:Key={_key}"], timeout.Token);
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "TestResults", "Aspire");
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, $"startup-{Guid.NewGuid():N}.log");
        builder.Services.AddLogging(logging => logging.AddProvider(new StartupLogProvider(logPath, _key)).SetMinimumLevel(LogLevel.Information));
        _application = await builder.BuildAsync(timeout.Token);
        try
        {
            await _application.StartAsync(timeout.Token);
            // This web resource waits for database-migrations in AppHost.cs.
            await _application.ResourceNotifications.WaitForResourceHealthyAsync("WarningSystems", timeout.Token);
            _client = _application.CreateHttpClient("WarningSystems", "http");
            _connectionString = await _application.GetConnectionStringAsync("PitstopDb", timeout.Token)
                ?? throw new InvalidOperationException("Aspire did not supply the test database connection.");
            // Wait for HTTP readiness, not only process startup.
            using var request = new HttpRequestMessage(HttpMethod.Get, "/__testing/session");
            foreach (var header in HeadersFor(TestUsers.ForRole("User"))) request.Headers.Add(header.Key, header.Value);
            using var response = await _client.SendAsync(request, timeout.Token);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception)
        {
            await _application.DisposeAsync();
            _application = null;
            throw new InvalidOperationException($"Aspire startup failed. See {logPath}", exception);
        }
    }

    public IReadOnlyDictionary<string, string> HeadersFor(TestUser user) => new Dictionary<string, string>
    {
        ["X-Warning-Test-Key"] = _key,
        ["X-Warning-Test-User"] = user.Profile
    };

    // Future data assertions use the SAME migrated database that Aspire gives the app.
    // Never reads a production connection from appsettings or developer user secrets.
    public SqlConnection OpenDatabaseConnection() => new(_connectionString
        ?? throw new InvalidOperationException("Aspire has not started the test database."));

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_application is not null) await _application.DisposeAsync();
    }
}
