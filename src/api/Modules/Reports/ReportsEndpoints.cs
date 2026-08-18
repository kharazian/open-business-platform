using System.Text;
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
                var reports = await reportManagement.ListAccessibleReportsAsync(httpContext.User, formId, permissionService, cancellationToken);
                return Results.Ok(new { items = reports });
            });
        });

        group.MapGet("/fields", async (
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
                var fields = await reportManagement.ListReportFieldsAsync(httpContext.User, formId, permissionService, cancellationToken);
                return Results.Ok(fields);
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

        group.MapGet("/{reportId:guid}", async (
            Guid formId,
            Guid reportId,
            ReportManagementService reportManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanListReportsAsync(permissionService, httpContext, formId, cancellationToken)
                || !await permissionService.CanAccessReportAsync(httpContext.User, reportId, PlatformPermissions.Report.View, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleReportRequestAsync(async () =>
            {
                var report = await reportManagement.GetListReportAsync(formId, reportId, cancellationToken);
                return Results.Ok(report);
            });
        });

        group.MapPut("/{reportId:guid}", async (
            Guid formId,
            Guid reportId,
            UpdateListReportRequest request,
            ReportManagementService reportManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageReportAsync(permissionService, httpContext, formId, reportId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleReportRequestAsync(async () =>
            {
                var report = await reportManagement.UpdateListReportAsync(formId, reportId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Ok(report);
            });
        });

        group.MapDelete("/{reportId:guid}", async (
            Guid formId,
            Guid reportId,
            ReportManagementService reportManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageReportAsync(permissionService, httpContext, formId, reportId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleReportRequestAsync(async () =>
            {
                await reportManagement.DeleteListReportAsync(formId, reportId, GetCurrentUserId(httpContext), cancellationToken);
                return Results.NoContent();
            });
        });

        group.MapGet("/{reportId:guid}/run", async (
            Guid formId,
            Guid reportId,
            int? page,
            int? pageSize,
            string? search,
            string? sortFieldId,
            string? sortDirection,
            ReportManagementService reportManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanListReportsAsync(permissionService, httpContext, formId, cancellationToken)
                || !await permissionService.CanAccessReportAsync(httpContext.User, reportId, PlatformPermissions.Report.View, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleReportRequestAsync(async () =>
            {
                var report = await reportManagement.ExecuteListReportAsync(
                    httpContext.User,
                    formId,
                    reportId,
                    new RunListReportRequest(page ?? 1, pageSize ?? 25, search, sortFieldId, sortDirection, GetReportFilterValues(httpContext)),
                    permissionService,
                    cancellationToken);

                return Results.Ok(report);
            });
        });

        group.MapGet("/{reportId:guid}/export.csv", async (
            Guid formId,
            Guid reportId,
            string? search,
            string? sortFieldId,
            string? sortDirection,
            ReportManagementService reportManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanExportReportsAsync(permissionService, httpContext, formId, cancellationToken)
                || !await permissionService.CanAccessReportAsync(httpContext.User, reportId, PlatformPermissions.Report.Export, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleReportRequestAsync(async () =>
            {
                var export = await reportManagement.ExportListReportCsvAsync(
                    httpContext.User,
                    formId,
                    reportId,
                    new RunListReportRequest(1, 100, search, sortFieldId, sortDirection, GetReportFilterValues(httpContext)),
                    GetCurrentUserId(httpContext),
                    permissionService,
                    cancellationToken);

                return Results.File(
                    Encoding.UTF8.GetBytes(export.Content),
                    ListReportCsvExporter.ContentType,
                    export.FileName);
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

    private static async Task<bool> CanManageReportAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        return await CanCreateReportsAsync(permissionService, httpContext, formId, cancellationToken)
            && await permissionService.CanAccessReportAsync(httpContext.User, reportId, PlatformPermissions.Report.Manage, cancellationToken);
    }

    private static async Task<bool> CanExportReportsAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Reports, cancellationToken)
            && await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.Export, cancellationToken);
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

    private static IReadOnlyDictionary<string, string?> GetReportFilterValues(HttpContext httpContext)
    {
        const string Prefix = "filter.";
        return httpContext.Request.Query
            .Where(pair => pair.Key.StartsWith(Prefix, StringComparison.Ordinal))
            .ToDictionary(
                pair => pair.Key[Prefix.Length..],
                pair => string.IsNullOrWhiteSpace(pair.Value.ToString()) ? null : pair.Value.ToString(),
                StringComparer.Ordinal);
    }
}
