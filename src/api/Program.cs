using OpenBusinessPlatform.Api.Configuration;
using OpenBusinessPlatform.Api.Modules.Dashboard;

DotEnv.LoadFromNearestFile();
EnvironmentConfiguration.ApplyDerivedValues();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection(ApplicationOptions.SectionName));
builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));

var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDevelopment");
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Open Business Platform API"
}));

app.MapDashboardEndpoints();

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
