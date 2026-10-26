using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.DataAccess.SOPDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Interceptors;
using WarningSystems.Core.Services.Interface;

namespace WarningSystems.Testing;

public sealed class WarningApplication : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    public Mock<IGraphUserDataAccess> Graph { get; } = SeedData.CreateGraph();
    public bool IsSqlServer { get; } = !string.Equals(Environment.GetEnvironmentVariable("TEST_DATABASE_PROVIDER"), "Sqlite", StringComparison.OrdinalIgnoreCase);
    private readonly string _sqlitePath = Path.Combine(Path.GetTempPath(), $"WarningTests_{Guid.NewGuid():N}.db");
    public string DatabaseName { get; } = $"WarningTests_{Guid.NewGuid():N}";

    public WarningApplication()
    {
        if (!IsSqlServer)
        {
            _connectionString = new SqliteConnectionStringBuilder { DataSource = _sqlitePath }.ConnectionString;
            return;
        }
        var configured = Environment.GetEnvironmentVariable("TEST_SQLSERVER")
            ?? throw new InvalidOperationException("Set TEST_SQLSERVER to an isolated SQL Server master connection (see tests/README.md). Tests never use application connection strings.");
        var connection = new SqlConnectionStringBuilder(configured);
        if (!string.Equals(connection.InitialCatalog, "master", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("TEST_SQLSERVER must target master. Each fixture creates its own uniquely named database.");
        connection.InitialCatalog = DatabaseName;
        _connectionString = connection.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<WarningSystemDbContext>>();
            services.RemoveAll<WarningSystemDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<WarningSystemDbContext>>();
            services.AddDbContext<WarningSystemDbContext>((sp, options) =>
            {
                ConfigureDatabase(options);
                options.AddInterceptors(sp.GetRequiredService<AdminAuditSaveChangesInterceptor>());
            });

            services.RemoveAll<IGraphUserDataAccess>();
            services.AddSingleton(Graph.Object);
            services.RemoveAll<IUserRoleService>();
            services.AddScoped<IUserRoleService, TestRoleService>();
            services.RemoveAll<ISOPDataAccess>();
            var sop = new Mock<ISOPDataAccess>(MockBehavior.Strict);
            sop.Setup(x => x.GetActiveDocumentsAsync()).ReturnsAsync([]);
            services.AddSingleton(sop.Object);
            services.RemoveAll<IAzureFileStorageDataAccess>();
            services.AddSingleton<IAzureFileStorageDataAccess, TestFileStorage>();

            // Remove real request-handler schemes too: OIDC otherwise initializes on
            // every request and requires Entra configuration before our fake runs.
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthentication>(TestAuthentication.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthentication.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthentication.SchemeName;
                options.DefaultChallengeScheme = TestAuthentication.SchemeName;
                options.DefaultForbidScheme = TestAuthentication.SchemeName;
            });
        });
    }

    // An independent context/connection: assertions do not read tracked application entities.
    public WarningSystemDbContext OpenDatabase()
    {
        var options = new DbContextOptionsBuilder<WarningSystemDbContext>();
        ConfigureDatabase(options);
        return new(options.Options);
    }

    private void ConfigureDatabase(DbContextOptionsBuilder options)
    {
        if (IsSqlServer) options.UseSqlServer(_connectionString);
        else
        {
            var connection = NewSqliteConnection();
            options.UseSqlite(connection, contextOwnsConnection: true);
        }
    }

    private SqliteConnection NewSqliteConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.CreateFunction("SYSUTCDATETIME", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture));
        return connection;
    }

    public DbConnection OpenSqlConnection() => IsSqlServer ? new SqlConnection(_connectionString) : NewSqliteConnection();

    public async Task InitializeDatabaseAsync()
    {
        await using var db = OpenDatabase();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InsertAsync(db);
        if (!IsSqlServer)
        {
            db.CategoryQuestions.AddRange(new[] { 1, 2, 31 }.SelectMany(category => new[] { 1, 2, 3 }.Select(question =>
                new WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities.CategoryQuestion
                { CategoryId = category, QuestionId = question, IsActive = true, IsRequired = question != 3, SortOrder = question, CreatedBy = SeedData.ManagerId })));
            await db.SaveChangesAsync();
            return;
        }
        // Install the real category-question procedure used by the application.
        var procedure = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "TestSchema", "CategoryQuestions.sql"));
        await db.Database.ExecuteSqlRawAsync(procedure.TrimStart('\uFEFF'));
        await db.Database.ExecuteSqlRawAsync("EXEC ws.sp_EnsureDefaultCategoryQuestions @CreatedBy = 'test-manager', @SetIsActive = 1");
    }

    public async Task DeleteDatabaseAsync()
    {
        await using var db = OpenDatabase();
        await db.Database.EnsureDeletedAsync();
        if (!IsSqlServer) SqliteConnection.ClearAllPools();
    }

    public HttpClient ClientFor(string roles)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthentication.RoleHeader, roles);
        return client;
    }
}
