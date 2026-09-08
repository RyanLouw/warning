using Database.Migrations;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HW.Database.Migrations;

public class Program
{
    private sealed record MigratorTag(string ConnectionKey, string Tag);

    private static readonly MigratorTag[] MigratorTags = [
        new("PitstopDb", TagNames.Pitstop)
    ];

    public static void Main(string[] args)
    {
        try
        {
            Log.Information("Starting Application");

            IConfigurationRoot configSettings = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

            //Run migrations for all tags
            foreach (var tag in MigratorTags)
            {
                Log.Information("Entered RunMigrations for {ConnectionKey}", tag.ConnectionKey);
                using IHost host = CreateHost(tag, configSettings, args);

                using var scope = host.Services.CreateScope();
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                MigrateUp(scope.ServiceProvider);
                Log.Information("Finished with {ConnectionKey}", tag.ConnectionKey);
            }

            Log.Information("All Migrations completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Main");
            throw;
        }
    }

    private static IHost CreateHost(
        MigratorTag migratorTag,
        IConfigurationRoot configSettings,
        string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configSettings);

        // Add runtime configuration again after the packaged central-config
        // defaults. Aspire supplies resource references through environment
        // variables, so ConnectionStrings__PitstopDb must have the final say.
        builder.Configuration
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.AddServiceDefaults();

        var connection = GetConnectionString(builder.Configuration, migratorTag.ConnectionKey);

        builder.Services.AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                rb.AddSqlServer()
                    .WithGlobalConnectionString(connection)
                    .ScanIn(typeof(Program).Assembly).For.Migrations();
            })
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .Configure<RunnerOptions>(opt => opt.Tags = [migratorTag.Tag])
            .AddSingleton(Log.Logger);

        var host = builder.Build();
        Log.Information("Application services built successfully");
        return host;
    }

    private static string GetConnectionString(IConfiguration configuration, string connectionKey)
    {
        var connection = configuration.GetConnectionString(connectionKey);

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{connectionKey}' is not configured.");
        }

        connection = connection.Trim();

        // Environment variables and user secrets are sometimes populated with
        // the JSON quotes around the value. Those quotes are not part of a SQL
        // Server connection string and cause SqlClient to fail at index zero.
        if (connection.Length >= 2 &&
            ((connection[0] == '"' && connection[^1] == '"') ||
             (connection[0] == '\'' && connection[^1] == '\'')))
        {
            connection = connection[1..^1].Trim();
        }

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{connectionKey}' is empty.");
        }

        return connection;
    }

    private static void MigrateUp(IServiceProvider serviceProvider)
    {
        IMigrationRunner runner = serviceProvider.GetRequiredService<IMigrationRunner>();

        runner.MigrateUp();
    }
}
