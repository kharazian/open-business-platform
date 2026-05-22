using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/forms/{formId:guid}/reports").WithTags("Reports").RequireAuthorization();

        group.MapGet("", async (
            Guid formId,
            ReportManagementService reportManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanListReportsAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleReportRequestAsync(async () =>
            {
                var reports = await reportManagement.ListReportsAsync(formId, cancellationToken);
                return Results.Ok(new { items = reports });
            });
        });

        group.MapPost("", async (
            Guid formId,
            CreateListReportRequest request,
            ReportManagementService reportManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanCreateReportsAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleReportRequestAsync(async () =>
            {
                var report = await reportManagement.CreateListReportAsync(formId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/forms/{formId}/reports/{report.Id}", report);
            });
        });

        return endpoints;
    }

    private static async Task<bool> CanListReportsAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Reports, cancellationToken)
            && await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.View, cancellationToken);
    }

    private static async Task<bool> CanCreateReportsAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Reports.Manage, cancellationToken)
            && await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.Manage, cancellationToken);
    }

    private static async Task<IResult> HandleReportRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ReportManagementException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new ReportErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
