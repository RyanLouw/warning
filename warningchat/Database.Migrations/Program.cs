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

            //Run migrations for all tags
            foreach (var tag in MigratorTags)
            {
                Log.Information("Entered RunMigrations for {ConnectionKey}", tag.ConnectionKey);
                IServiceProvider serviceProvider = CreateServices(tag);

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
            throw;
        }
    }

    private static IServiceProvider CreateServices(MigratorTag migratorTag)
    {
        var builder = Host.CreateApplicationBuilder();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.AddServiceDefaults();

        var connection = builder.Configuration.GetConnectionString(migratorTag.ConnectionKey);
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                $"Connection string '{migratorTag.ConnectionKey}' was not provided. " +
                $"Run the migrations through the AppHost or set " +
                $"ConnectionStrings__{migratorTag.ConnectionKey}.");
        }

        builder.Services.AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                rb.AddSqlServer()
                    .WithGlobalConnectionString(connection)
                    .ScanIn(typeof(Program).Assembly)
                        .For.Migrations()
                        .For.EmbeddedResources();
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
