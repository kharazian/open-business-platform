using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Configuration;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Dashboard;
using OpenBusinessPlatform.Api.Modules.Dashboards;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Notifications;
using OpenBusinessPlatform.Api.Modules.Records;
using OpenBusinessPlatform.Api.Modules.Reports;
using OpenBusinessPlatform.Api.Modules.Triggers;
using OpenBusinessPlatform.Api.Platform;
using Scalar.AspNetCore;

DotEnv.LoadFromNearestFile();
EnvironmentConfiguration.ApplyDerivedValues();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection(ApplicationOptions.SectionName));
builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.Configure<LocalAuthenticationOptions>(builder.Configuration.GetSection(LocalAuthenticationOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<PasswordRecoveryOptions>(builder.Configuration.GetSection(PasswordRecoveryOptions.SectionName));
builder.Services.AddDbContext<OpenBusinessPlatformDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});
builder.Services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(EfRepository<,>));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
builder.Services.AddSingleton<BootstrapAdminUserDirectory>();
builder.Services.AddSingleton<LocalPasswordHasher>();
builder.Services.AddSingleton<PasswordResetTokenGenerator>();
builder.Services.AddSingleton<PasswordResetTokenHasher>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IdentityManagementService>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<FormManagementService>();
builder.Services.AddScoped<RecordSubmissionService>();
builder.Services.AddScoped<RecordQueryService>();
builder.Services.AddScoped<RecordMutationService>();
builder.Services.AddScoped<ReportManagementService>();
builder.Services.AddScoped<TriggerDefinitionService>();
builder.Services.AddScoped<TriggerActionRegistry>();
builder.Services.AddScoped<TriggerExecutionService>();
builder.Services.AddScoped<TriggerEventDispatcher>();
builder.Services.AddScoped<DashboardSummaryService>();
builder.Services.AddScoped<ChartAggregationService>();
builder.Services.AddScoped<DashboardDefinitionService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        var authOptions = builder.Configuration
            .GetSection(LocalAuthenticationOptions.SectionName)
            .Get<LocalAuthenticationOptions>() ?? new LocalAuthenticationOptions();

        options.Cookie.HttpOnly = true;
        options.Cookie.Name = string.IsNullOrWhiteSpace(authOptions.CookieName) ? "obp.auth" : authOptions.CookieName;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || !authOptions.RequireSecureCookies
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Open Business Platform API v1");
    });
    app.MapScalarApiReference();
    app.UseCors("LocalDevelopment");
    await DemoDataSeeder.SeedDevelopmentAsync(app.Services);
}
else
{
    app.UseForwardedHeaders();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Open Business Platform API"
}));

app.MapPlatformApiModules();

app.Run();

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .GetChildren()
        .Select(origin => origin.Value)
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin!)
        .ToArray();

    return configuredOrigins.Length > 0
        ? configuredOrigins
        : new[] { "http://localhost:5174", "http://127.0.0.1:5174" };
}
