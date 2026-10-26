using WarningSystems.Testing;

namespace WarningSystems.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    public WarningApplication App { get; } = new();
    public Task InitializeAsync() => App.InitializeDatabaseAsync();
    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
        await App.DeleteDatabaseAsync();
    }
}
