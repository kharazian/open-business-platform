using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard").WithTags("Dashboard");
        group.RequireAuthorization();

        group.MapGet("/summary", async (
            DashboardSummaryService dashboardSummary,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Dashboard, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(await dashboardSummary.GetSummaryAsync(cancellationToken));
        });

        return endpoints;
    }
}
