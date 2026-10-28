using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
var testing = builder.Configuration.GetValue<bool>("Testing:Enabled");
#if WARNING_SYSTEM_TEST_BUILD
if (!testing)
    throw new InvalidOperationException("A test build must be started by WarningSystemTest with Testing:Enabled=true.");
#else
if (testing)
    throw new InvalidOperationException("Testing requires a WarningSystemTestingBuild=true build.");
#endif

// Same Aspire resources and migrations in both modes. Test runs get a session
// container, random port, and no persistent volume; development data is preserved.
var server = testing
    ? builder.AddSqlServer("WarningSystems-sql")
    : builder.AddSqlServer("WarningSystems-sql", port: 2433)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("WarningSystems-sql");
var sql = server.AddDatabase("PitstopDb", "PitStop");

var migrations = builder.AddProject<Projects.Database_Migrations>("database-migrations")
    .WithEnvironment("DOTNET_ENVIRONMENT", testing ? "Testing" : "Staging")
    .WithReference(sql)
    .WaitFor(sql);

var web = builder.AddProject<Projects.WarningSystems>("WarningSystems", launchProfileName: "http")
    .WithReference(sql)
    .WaitForCompletion(migrations);

if (testing)
{
    var key = builder.Configuration["Testing:Key"];
    if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        throw new InvalidOperationException("The test runner must supply a per-run authentication key.");
    web.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Testing")
        .WithEnvironment("WarningSystemTesting__Key", key)
        .WithHttpHealthCheck("/__testing/health", endpointName: "http");
}

await builder.Build().RunAsync();
