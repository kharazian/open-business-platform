using Microsoft.AspNetCore.Authentication.Cookies;
using OpenBusinessPlatform.Api.Configuration;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Platform;

DotEnv.LoadFromNearestFile();
EnvironmentConfiguration.ApplyDerivedValues();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection(ApplicationOptions.SectionName));
builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.AddSingleton<BootstrapAdminUserDirectory>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "obp.auth";
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
