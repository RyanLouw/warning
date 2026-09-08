using Database.Migrations;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using HW.CentralConfig.Package.Core;
namespace HW.Database.Migrations;

public class Program
{
    private sealed record MigratorTag(string ConnectionKey, string Tag);

    private static readonly MigratorTag[] MigratorTags = [
        new("PitstopDb", TagNames.Pitstop)
    ];

    public static async Task Main(string[] args)
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
                IServiceProvider serviceProvider = await CreateServices(tag, configSettings);

                using var scope = serviceProvider.CreateScope();
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                MigrateUp(scope.ServiceProvider);
                Log.Information("Finished with {ConnectionKey}", tag.ConnectionKey);
            }

            Log.Information("All Migrations completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Main");
        }
    }


    private static async Task<IServiceProvider> CreateServices(MigratorTag migratorTag, IConfigurationRoot configSettings)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configSettings);

        await builder.AddCentralConfigAsync();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configSettings)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.AddServiceDefaults();

        var connection = builder.Configuration.GetConnectionString(migratorTag.ConnectionKey);

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

        var app = builder.Build();
        Log.Information("Application services built successfully");
        return app.Services;
    }

    private static void MigrateUp(IServiceProvider serviceProvider)
    {
        IMigrationRunner runner = serviceProvider.GetRequiredService<IMigrationRunner>();

        runner.MigrateUp();
    }
}