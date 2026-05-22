using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Configuration;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Records;
using OpenBusinessPlatform.Api.Platform;
using Scalar.AspNetCore;

DotEnv.LoadFromNearestFile();
EnvironmentConfiguration.ApplyDerivedValues();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection(ApplicationOptions.SectionName));
builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.Configure<LocalAuthenticationOptions>(builder.Configuration.GetSection(LocalAuthenticationOptions.SectionName));
builder.Services.AddDbContext<OpenBusinessPlatformDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});
builder.Services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(EfRepository<,>));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
builder.Services.AddSingleton<BootstrapAdminUserDirectory>();
builder.Services.AddSingleton<LocalPasswordHasher>();
builder.Services.AddScoped<IdentityManagementService>();
builder.Services.AddScoped<FormManagementService>();
builder.Services.AddScoped<RecordSubmissionService>();
builder.Services.AddScoped<RecordQueryService>();
builder.Services.AddScoped<PermissionService>();
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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
