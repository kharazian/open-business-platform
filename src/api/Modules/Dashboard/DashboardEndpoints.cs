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

        group.MapPost("/analytics/run", async (
            DashboardAnalyticsRequest request,
            DashboardAnalyticsService dashboardAnalytics,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Dashboard, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleDashboardAnalyticsRequestAsync(async () =>
            {
                var result = await dashboardAnalytics.RunAsync(httpContext.User, request, permissionService, cancellationToken);
                return Results.Ok(result);
            });
        });

        endpoints.MapPost("/api/forms/{formId:guid}/chart-widgets/preview", async (
            Guid formId,
            ChartWidgetConfigDefinition request,
            ChartAggregationService chartAggregation,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanPreviewChartAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleChartRequestAsync(async () =>
            {
                var preview = await chartAggregation.PreviewAsync(httpContext.User, formId, request, permissionService, cancellationToken);
                return Results.Ok(preview);
            });
        }).WithTags("Dashboard").RequireAuthorization();

        return endpoints;
    }

    private static async Task<bool> CanPreviewChartAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Reports, cancellationToken)
            && await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.View, cancellationToken);
    }

    private static async Task<IResult> HandleChartRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ChartAggregationException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new ChartErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
    }

    private static async Task<IResult> HandleDashboardAnalyticsRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (DashboardAnalyticsException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new DashboardAnalyticsErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
    }
}
