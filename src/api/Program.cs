using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Configuration;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Dashboard;
using OpenBusinessPlatform.Api.Modules.Dashboards;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Integrations;
using OpenBusinessPlatform.Api.Modules.Notifications;
using OpenBusinessPlatform.Api.Modules.Printing;
using OpenBusinessPlatform.Api.Modules.Records;
using OpenBusinessPlatform.Api.Modules.Reports;
using OpenBusinessPlatform.Api.Modules.Triggers;
using OpenBusinessPlatform.Api.Modules.Workflows;
using OpenBusinessPlatform.Api.Modules.Workspaces;
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
builder.Services.Configure<AutomationHealthOptions>(builder.Configuration.GetSection(AutomationHealthOptions.SectionName));
builder.Services.AddDbContext<OpenBusinessPlatformDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});
builder.Services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(EfRepository<,>));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IWorkspaceContext, HttpContextWorkspaceContext>();
builder.Services.AddScoped<WorkspaceContextService>();
builder.Services.AddScoped<WorkspaceMembershipService>();
builder.Services.AddScoped<SsoProviderService>();
builder.Services.AddScoped<OidcSsoService>();
builder.Services.AddSingleton<BootstrapAdminUserDirectory>();
builder.Services.AddSingleton<LocalPasswordHasher>();
builder.Services.AddSingleton<PasswordResetTokenGenerator>();
builder.Services.AddSingleton<PasswordResetTokenHasher>();
builder.Services.AddSingleton<IntegrationApiKeyHasher>();
builder.Services.AddSingleton<IntegrationApiKeyGenerator>();
builder.Services.AddSingleton<IncomingWebhookListenerSecretHasher>();
builder.Services.AddSingleton<IncomingWebhookListenerSecretGenerator>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<NotificationQueryService>();
builder.Services.AddScoped<IdentityManagementService>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<FormManagementService>();
builder.Services.AddScoped<RecordSubmissionService>();
builder.Services.AddScoped<RecordQueryService>();
builder.Services.AddScoped<RecordMutationService>();
builder.Services.AddScoped<RecordLookupService>();
builder.Services.AddScoped<RecordRelationshipService>();
builder.Services.AddScoped<AutonumberService>();
builder.Services.AddScoped<FileAttachmentService>();
builder.Services.AddScoped<IFileAttachmentContentStore, PostgresFileAttachmentContentStore>();
builder.Services.AddSingleton<IFileAttachmentScanner, DeterministicFileAttachmentScanner>();
builder.Services.AddScoped<RecordTimelineService>();
builder.Services.AddScoped<PrintTemplateService>();
builder.Services.AddScoped<PrintPdfService>();
builder.Services.AddScoped<DefaultReportProvisioningService>();
builder.Services.AddScoped<ReportManagementService>();
builder.Services.AddScoped<TriggerDefinitionService>();
builder.Services.AddScoped<TriggerActionRegistry>();
builder.Services.AddScoped<TriggerPdfAttachmentService>();
builder.Services.AddScoped<TriggerExecutionService>();
builder.Services.AddScoped<TriggerEventDispatcher>();
builder.Services.AddScoped<TriggerEventOutbox>();
builder.Services.AddScoped<TriggerEventOutboxProcessor>();
builder.Services.AddScoped<TriggerEventOutboxOperationsService>();
builder.Services.AddScoped<TriggerEventOutboxRetentionService>();
builder.Services.AddScoped<AutomationOutboxSnapshotService>();
builder.Services.AddScoped<TriggerAutomaticRetryService>();
builder.Services.AddScoped<TriggerScheduleService>();
builder.Services.AddHostedService<TriggerRetryWorker>();
builder.Services.AddHostedService<TriggerScheduleWorker>();
builder.Services.AddHostedService<TriggerEventOutboxWorker>();
builder.Services.AddHostedService<TriggerEventOutboxRetentionWorker>();
builder.Services.AddHostedService<AutomationOutboxMonitorWorker>();
builder.Services.AddScoped<WorkflowDefinitionService>();
builder.Services.AddScoped<WorkflowActionExecutionService>();
builder.Services.AddScoped<WorkflowApprovalService>();
builder.Services.AddScoped<RecordWorkflowService>();
builder.Services.AddScoped<DashboardSummaryService>();
builder.Services.AddScoped<ChartAggregationService>();
builder.Services.AddScoped<DashboardAnalyticsService>();
builder.Services.AddScoped<DashboardDefinitionService>();
builder.Services.AddScoped<IntegrationApiKeyService>();
builder.Services.AddScoped<IntegrationConnectorService>();
builder.Services.AddScoped<IntegrationLogService>();
builder.Services.AddScoped<PublicRecordApiService>();
builder.Services.AddScoped<IncomingWebhookListenerService>();
builder.Services.AddScoped<IncomingWebhookExecutionService>();
builder.Services.AddScoped<RecordImportJobService>();
builder.Services.AddScoped<ExternalExportJobService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<AccessPolicyEvaluator>();
builder.Services.AddScoped<AccessPolicyService>();
builder.Services.AddScoped<RetentionService>();
builder.Services.AddScoped<AdministrativeBackupService>();
builder.Services.AddScoped<WorkspaceBrandingService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<CustomDomainService>();
builder.Services.AddScoped<ComplianceService>();
builder.Services.AddHttpClient<IDnsTxtResolver, CloudflareDnsTxtResolver>();
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
    })
    .AddScheme<AuthenticationSchemeOptions, IntegrationApiKeyAuthenticationHandler>(
        IntegrationApiKeyAuthenticationDefaults.AuthenticationScheme,
        _ => { });
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks()
    .AddCheck<AutomationOutboxHealthCheck>("automation_outbox", tags: new[] { "automation" });
builder.Services.AddOpenApi();
builder.Services.AddHttpClient("trigger-webhooks");
builder.Services.AddHttpClient("oidc-discovery");
builder.Services.AddHttpClient("oidc-token");

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

app.Use(async (httpContext, next) =>
{
    try
    {
        await next(httpContext);
    }
    catch (DbUpdateConcurrencyException) when (!httpContext.Response.HasStarted)
    {
        httpContext.Response.Clear();
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            message = "The resource was changed by another request. Refresh and try again."
        }, httpContext.RequestAborted);
    }
});

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
app.UseMiddleware<CustomDomainResolutionMiddleware>();
app.UseMiddleware<WorkspaceMembershipMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Open Business Platform API"
}));

app.MapHealthChecks("/health/automation", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("automation"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = async (httpContext, report) =>
    {
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString().ToLowerInvariant(),
                description = entry.Value.Description
            })
        }, httpContext.RequestAborted);
    }
});

app.MapGet("/metrics", async (
    HttpContext httpContext,
    AutomationOutboxSnapshotService snapshots,
    IOptions<AutomationHealthOptions> configuredOptions,
    IHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    var options = configuredOptions.Value.Normalize();

    if (!options.MetricsEnabled)
    {
        return Results.NotFound();
    }

    if (!AutomationMetrics.IsAccessAllowed(
            environment.IsDevelopment(),
            httpContext.Request.Headers.Authorization.FirstOrDefault(),
            options))
    {
        return Results.Unauthorized();
    }

    var snapshot = await snapshots.GetSnapshotAsync(cancellationToken);
    return Results.Text(AutomationMetrics.Format(snapshot, options), AutomationMetrics.ContentType);
});

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
