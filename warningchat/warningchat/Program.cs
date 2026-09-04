using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Serilog;
using WarningSystems.core.Models.DTO;
using WarningSystems.core.Services.Interface;
using WarningSystems.Core.Auth;
using WarningSystems.Core.Auth.Interface;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.DataAccess.GraphDataAccess.Context;
using WarningSystems.Core.DataAccess.SOPDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Interceptors;
using WarningSystems.Core.Manager;
using WarningSystems.Core.Services;
using WarningSystems.Core.Services.Interface;
using WarningSystems.Core.ViewModels;
using WarningSystems.Middleware;
using WarningSystems.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) =>
{
    config.ReadFrom.Configuration(ctx.Configuration);
});

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.AccessDeniedPath = "/Home/AccessDenied";
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var tenantId = config["AzureAd:TenantId"];
    var clientId = config["AzureAd:ClientId"];
    var clientSecret = config["AzureAd:ClientSecret"];

    var credential = new ClientSecretCredential(
        tenantId,
        clientId,
        clientSecret
    );

    return new GraphServiceClient(
        credential,
        new[] { "https://graph.microsoft.com/.default" }
    );
});

builder.Services.AddSingleton<IAppGraphClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var tenantId = config["AzureAd:TenantId"];
    var clientId = config["AzureAd:ClientId"];
    var clientSecret = config["AzureAd:ClientSecret"];

    var credential = new ClientSecretCredential(
        tenantId,
        clientId,
        clientSecret
    );

    var client = new GraphServiceClient(
        credential,
        new[] { "https://graph.microsoft.com/.default" }
    );

    return new AppGraphClient(client);
});

builder.Services.AddScoped<IUserGraphClient>(sp =>
{
    var tokenAcquisition = sp.GetRequiredService<ITokenAcquisition>();
    var config = sp.GetRequiredService<IConfiguration>();

    var scopes = (config["Graph:Scopes"] ?? "User.Read")
        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

    var tokenProvider = new TokenAcquisitionTokenProvider(tokenAcquisition, scopes);
    var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

    var graphClient = new GraphServiceClient(authProvider);

    return new UserGraphClient(graphClient);
});

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "RequestVerificationToken";
});

builder.Services.Configure<ApplicationOptions>(
    builder.Configuration.GetSection("Application"));

builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddTransient<IClaimsTransformation, AppRoleClaimsTransformation>();

/// database services would go here
builder.Services.AddDbContext<WarningSystemDbContext>(
    (serviceProvider, options) =>
    {
        var auditInterceptor =
            serviceProvider
                .GetRequiredService<AdminAuditSaveChangesInterceptor>();

        options.UseSqlServer(
            builder.Configuration.GetConnectionString("PitstopDb"));

        options.AddInterceptors(auditInterceptor);
    });


builder.Services.AddSingleton<
    IEmailTemplateRenderer,
    EmbeddedEmailTemplateRenderer>();


builder.Services.AddScoped<
    IWarningSystemDataAccess,
    WarningSystemDataAccess>();

builder.Services.AddDbContext<SopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SopDb")));
builder.Services.AddScoped<
    ISOPDataAccess,
    SOPDataAccess>();

builder.Services.AddScoped<
    IGraphUserDataAccess,
    GraphUserDataAccess>();

builder.Services.AddScoped<AdminAuditSaveChangesInterceptor>();
IdentityModelEventSource.ShowPII = true;

/// manager services would go here
builder.Services.AddScoped<ITransgressionManager, TransgressionManager>();

var conn = builder.Configuration["Storage:ConnectionString"]
           ?? throw new InvalidOperationException("Storage connection string missing.");

builder.Services.AddScoped<IAzureFileStorageDataAccess>(
    _ => new AzureFileStorageDataAccess(conn)
);

builder.Services.AddScoped<IWarningPdfExportService, WarningPdfExportService>();
builder.Services.AddScoped<WarningOverviewMapperServise>();

var app = builder.Build();

// --------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Custom exception handling
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();