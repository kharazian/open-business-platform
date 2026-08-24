using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public static class DashboardsEndpoints
{
    public static IEndpointRouteBuilder MapDashboardsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboards").WithTags("Dashboards").RequireAuthorization();

        group.MapGet("/navigation", async (DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanViewDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(new { items = await dashboards.ListNavigationAsync(await BuildAccessContextAsync(permissionService, httpContext, cancellationToken), cancellationToken) }));
        });

        group.MapGet("/by-slug/{slug}", async (string slug, DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanViewDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.GetBySlugAsync(slug, await BuildAccessContextAsync(permissionService, httpContext, cancellationToken), cancellationToken)));
        });

        group.MapGet("/sharing-options", async (DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.GetSharingOptionsAsync(cancellationToken)));
        });

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
                var accessContext = await BuildAccessContextAsync(permissionService, httpContext, cancellationToken);
                var items = await dashboards.ListAsync(accessContext, cancellationToken);
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

            return await HandleDashboardRequestAsync(async () =>
            {
                var accessContext = await BuildAccessContextAsync(permissionService, httpContext, cancellationToken);
                return Results.Ok(await dashboards.GetAsync(dashboardId, accessContext, cancellationToken));
            });
        });

        group.MapGet("/{dashboardId:guid}/revisions", async (Guid dashboardId, DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(new { items = await dashboards.ListRevisionsAsync(dashboardId, cancellationToken) }));
        });

        group.MapGet("/{dashboardId:guid}/sharing", async (Guid dashboardId, DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.GetSharingAsync(dashboardId, cancellationToken)));
        });

        group.MapGet("/{dashboardId:guid}/published-comparison", async (Guid dashboardId, DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.GetPublishedComparisonAsync(dashboardId, cancellationToken)));
        });

        group.MapPost("/{dashboardId:guid}/revisions/{revisionId:guid}/restore", async (Guid dashboardId, Guid revisionId, DashboardRevisionRestoreRequest request, DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.RestoreRevisionAsync(dashboardId, revisionId, request, GetCurrentUserId(httpContext), cancellationToken)));
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

        group.MapPost("/{dashboardId:guid}/publish", async (Guid dashboardId, DashboardPublicationMutationRequest request, DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.PublishAsync(dashboardId, request, GetCurrentUserId(httpContext), cancellationToken)));
        });

        group.MapPost("/{dashboardId:guid}/unpublish", async (Guid dashboardId, DashboardPublicationMutationRequest request, DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () => Results.Ok(await dashboards.UnpublishAsync(dashboardId, request, GetCurrentUserId(httpContext), cancellationToken)));
        });

        group.MapDelete("/{dashboardId:guid}", async (Guid dashboardId, [Microsoft.AspNetCore.Mvc.FromBody] DashboardPublicationMutationRequest request, DashboardDefinitionService dashboards, PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await CanManageDashboardsAsync(permissionService, httpContext, cancellationToken)) return Results.Forbid();
            return await HandleDashboardRequestAsync(async () =>
            {
                await dashboards.DeleteAsync(dashboardId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.NoContent();
            });
        });

        return endpoints;
    }

    private static async Task<bool> CanViewDashboardsAsync(PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Dashboard, cancellationToken);
    }

    private static async Task<bool> CanManageDashboardsAsync(PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Dashboards.Manage, cancellationToken);
    }

    private static async Task<DashboardAccessContext> BuildAccessContextAsync(PermissionService permissionService, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var permissions = await permissionService.GetEffectivePermissionsAsync(httpContext.User, cancellationToken);
        var subjects = await permissionService.GetSubjectIdsAsync(httpContext.User, cancellationToken);
        return new DashboardAccessContext(GetCurrentUserId(httpContext),
            permissions.Contains(PlatformPermissions.Dashboards.Manage), permissions.ToHashSet(StringComparer.Ordinal), subjects.RoleIds, subjects.GroupIds);
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
