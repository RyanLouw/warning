using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using CallQualityUITesting.SystemUnderTest;
using Microsoft.Extensions.Logging;
using Xunit;

[assembly: AssemblyFixture(typeof(CallQualityServiceSut))]

namespace CallQualityUITesting.SystemUnderTest;

public sealed class CallQualityServiceSut : IAsyncLifetime
{
    private DistributedApplication? _sut;

    public static TimeSpan DefaultTimeout { get; }
        = TimeSpan.FromMinutes(5);

    public HttpClient HttpClient { get; private set; } = null!;

    public string BaseUrl =>
        HttpClient.BaseAddress?.ToString()
        ?? throw new InvalidOperationException(
            "CallQuality base URL is not available.");

    public async ValueTask InitializeAsync()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var appHost =
            await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.HW_CallMonitor_System_AppHost>(
                    [
                        "DcpPublisher:RandomizePorts=false"
                    ],
                    cancellationToken);

        _sut = await appHost.BuildAsync();

        await _sut.StartAsync(cancellationToken);

        await _sut.ResourceCommands.ExecuteCommandAsync(
            "hw-CallQuality",
            KnownResourceCommands.StartCommand,
            cancellationToken);

        HttpClient = _sut.CreateHttpClient(
            "hw-CallQuality",
            "https");

        Console.WriteLine(
            $"CallQuality test URL: {BaseUrl}");
    }

    public HttpClient GetHttpClient()
    {
        var client = HttpClient;

        client.DefaultRequestHeaders.Remove(
            "X-Correlation-ID");

        client.DefaultRequestHeaders.Add(
            "X-Correlation-ID",
            Guid.NewGuid().ToString());

        if (!client.DefaultRequestHeaders.Contains(
                "X-API-Key"))
        {
            client.DefaultRequestHeaders.Add(
                "X-API-Key",
                "");
        }

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (_sut is not null)
        {
            await _sut.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}