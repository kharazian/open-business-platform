using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public static class DashboardsEndpoints
{
    public static IEndpointRouteBuilder MapDashboardsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboards").WithTags("Dashboards").RequireAuthorization();

        group.MapGet("", async (
            DashboardDefinitionService dashboards,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanViewDashboardsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleDashboardRequestAsync(async () =>
            {
                var items = await dashboards.ListAsync(cancellationToken);
                return Results.Ok(new { items });
            });
        });

        group.MapGet("/{dashboardId:guid}", async (
            Guid dashboardId,
            DashboardDefinitionService dashboards,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanViewDashboardsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.GetAsync(dashboardId, cancellationToken)));
        });

        group.MapPost("", async (
            CreateDashboardRequest request,
            DashboardDefinitionService dashboards,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleDashboardRequestAsync(async () =>
            {
                var dashboard = await dashboards.CreateAsync(request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/dashboards/{dashboard.Id}", dashboard);
            });
        });

        group.MapPut("/{dashboardId:guid}", async (
            Guid dashboardId,
            UpdateDashboardRequest request,
            DashboardDefinitionService dashboards,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.UpdateAsync(dashboardId, request, GetCurrentUserId(httpContext), cancellationToken)));
        });

        return endpoints;
    }

    private static async Task<bool> CanViewDashboardsAsync(PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Dashboard, cancellationToken);
    }

    private static async Task<bool> CanManageDashboardsAsync(PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Reports.Manage, cancellationToken);
    }

    private static async Task<IResult> HandleDashboardRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (DashboardDefinitionException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new DashboardErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
