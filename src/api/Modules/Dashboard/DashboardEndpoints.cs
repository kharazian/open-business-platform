namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard").WithTags("Dashboard");
        group.RequireAuthorization();

        group.MapGet("/summary", () => Results.Ok(new DashboardSummaryResponse(
            "Open Business Platform",
            new[]
            {
                new DashboardMetric("Users", 0),
                new DashboardMetric("Roles", 0),
                new DashboardMetric("Permissions", 0),
                new DashboardMetric("Audit logs", 0)
            }
        )));

        return endpoints;
    }
}

public sealed record DashboardSummaryResponse(string Title, IReadOnlyCollection<DashboardMetric> Metrics);

public sealed record DashboardMetric(string Label, int Value);
