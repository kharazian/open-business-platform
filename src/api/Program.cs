using OpenBusinessPlatform.Api.Modules.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy
            .WithOrigins("http://localhost:5174")
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
