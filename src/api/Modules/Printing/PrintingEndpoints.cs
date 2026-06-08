using System.Security.Claims;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Records;
using OpenBusinessPlatform.Api.Modules.Reports;

namespace OpenBusinessPlatform.Api.Modules.Printing;

public static class PrintingEndpoints
{
    public static IEndpointRouteBuilder MapPrintingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var formGroup = endpoints.MapGroup("/api/forms/{formId:guid}/print-templates").WithTags("Printing").RequireAuthorization();

        formGroup.MapGet("", async (
            Guid formId,
            string? type,
            Guid? reportId,
            PrintTemplateService templates,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanViewFormTemplatesAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var items = await templates.ListAsync(formId, type, reportId, cancellationToken);
                var viewableItems = await PrintTemplateAuthorization.FilterViewableTemplatesAsync(
                    items,
                    (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                        httpContext.User,
                        targetReportId,
                        PlatformPermissions.Report.View,
                        targetCancellationToken),
                    cancellationToken);
                return Results.Ok(new { items = viewableItems });
            });
        });

        formGroup.MapPost("", async (
            Guid formId,
            CreatePrintTemplateRequest request,
            PrintTemplateService templates,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageFormTemplatesAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandlePrintTemplateRequestAsync(async () =>
            {
                if (!await PrintTemplateAuthorization.CanManageRequestedReportTemplateAsync(
                    request.Type,
                    request.ReportId,
                    (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                        httpContext.User,
                        targetReportId,
                        PlatformPermissions.Report.Manage,
                        targetCancellationToken),
                    cancellationToken))
                {
                    return Results.Forbid();
                }

                var template = await templates.CreateAsync(formId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/print-templates/{template.Id}", template);
            });
        });

        var templateGroup = endpoints.MapGroup("/api/print-templates").WithTags("Printing").RequireAuthorization();

        templateGroup.MapGet("/{templateId:guid}", async (
            Guid templateId,
            PrintTemplateService templates,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var template = await templates.GetAsync(templateId, cancellationToken);
                if (!await CanViewFormTemplatesAsync(permissionService, httpContext, template.FormId, cancellationToken))
                {
                    return Results.Forbid();
                }

                if (!await PrintTemplateAuthorization.CanViewTemplateAsync(
                    template,
                    (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                        httpContext.User,
                        targetReportId,
                        PlatformPermissions.Report.View,
                        targetCancellationToken),
                    cancellationToken))
                {
                    return Results.Forbid();
                }

                return Results.Ok(template);
            });
        });

        templateGroup.MapGet("/{templateId:guid}/versions", async (
            Guid templateId,
            PrintTemplateService templates,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var template = await templates.GetAsync(templateId, cancellationToken);
                if (!await CanViewFormTemplatesAsync(permissionService, httpContext, template.FormId, cancellationToken))
                {
                    return Results.Forbid();
                }

                if (!await PrintTemplateAuthorization.CanViewTemplateAsync(
                    template,
                    (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                        httpContext.User,
                        targetReportId,
                        PlatformPermissions.Report.View,
                        targetCancellationToken),
                    cancellationToken))
                {
                    return Results.Forbid();
                }

                return Results.Ok(new { items = await templates.ListVersionsAsync(templateId, cancellationToken) });
            });
        });

        templateGroup.MapPost("/{templateId:guid}/versions", async (
            Guid templateId,
            PublishPrintTemplateRequest request,
            PrintTemplateService templates,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var existing = await templates.GetAsync(templateId, cancellationToken);
                if (!await CanManageFormTemplatesAsync(permissionService, httpContext, existing.FormId, cancellationToken))
                {
                    return Results.Forbid();
                }

                if (!await PrintTemplateAuthorization.CanManageTemplateAsync(
                    existing,
                    (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                        httpContext.User,
                        targetReportId,
                        PlatformPermissions.Report.Manage,
                        targetCancellationToken),
                    cancellationToken))
                {
                    return Results.Forbid();
                }

                return Results.Ok(await templates.PublishAsync(templateId, request, GetCurrentUserId(httpContext), cancellationToken));
            });
        });

        templateGroup.MapPut("/{templateId:guid}", async (
            Guid templateId,
            UpdatePrintTemplateRequest request,
            PrintTemplateService templates,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var existing = await templates.GetAsync(templateId, cancellationToken);
                if (!await CanManageFormTemplatesAsync(permissionService, httpContext, existing.FormId, cancellationToken))
                {
                    return Results.Forbid();
                }

                if (!await PrintTemplateAuthorization.CanManageTemplateAsync(
                    existing,
                    (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                        httpContext.User,
                        targetReportId,
                        PlatformPermissions.Report.Manage,
                        targetCancellationToken),
                    cancellationToken)
                    || !await PrintTemplateAuthorization.CanManageRequestedReportTemplateAsync(
                        request.Type,
                        request.ReportId,
                        (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                            httpContext.User,
                            targetReportId,
                            PlatformPermissions.Report.Manage,
                            targetCancellationToken),
                        cancellationToken))
                {
                    return Results.Forbid();
                }

                return Results.Ok(await templates.UpdateAsync(templateId, request, GetCurrentUserId(httpContext), cancellationToken));
            });
        });

        templateGroup.MapDelete("/{templateId:guid}", async (
            Guid templateId,
            PrintTemplateService templates,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var existing = await templates.GetAsync(templateId, cancellationToken);
                if (!await CanManageFormTemplatesAsync(permissionService, httpContext, existing.FormId, cancellationToken))
                {
                    return Results.Forbid();
                }

                if (!await PrintTemplateAuthorization.CanManageTemplateAsync(
                    existing,
                    (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                        httpContext.User,
                        targetReportId,
                        PlatformPermissions.Report.Manage,
                        targetCancellationToken),
                    cancellationToken))
                {
                    return Results.Forbid();
                }

                await templates.DeleteAsync(templateId, GetCurrentUserId(httpContext), cancellationToken);
                return Results.NoContent();
            });
        });

        var versionGroup = endpoints.MapGroup("/api/print-template-versions").WithTags("Printing").RequireAuthorization();

        versionGroup.MapGet("/{versionId:guid}", async (
            Guid versionId,
            PrintTemplateService templates,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var version = await templates.GetVersionAsync(versionId, cancellationToken);
                if (!await CanViewFormTemplatesAsync(permissionService, httpContext, version.FormId, cancellationToken))
                {
                    return Results.Forbid();
                }

                if (!await PrintTemplateAuthorization.CanViewVersionAsync(
                    version,
                    (targetReportId, targetCancellationToken) => permissionService.CanAccessReportAsync(
                        httpContext.User,
                        targetReportId,
                        PlatformPermissions.Report.View,
                        targetCancellationToken),
                    cancellationToken))
                {
                    return Results.Forbid();
                }

                return Results.Ok(version);
            });
        });

        versionGroup.MapGet("/{versionId:guid}/records/{recordId:guid}.pdf", async (
            Guid versionId,
            Guid recordId,
            PrintTemplateService templates,
            PrintPdfService pdfService,
            RecordQueryService records,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var version = await templates.GetVersionAsync(versionId, cancellationToken);

                if (!string.Equals(version.Type, PrintTemplateTypes.Record, StringComparison.Ordinal))
                {
                    return Results.Json(new PrintTemplateErrorResponse("Print template version is not a record template."), statusCode: StatusCodes.Status400BadRequest);
                }

                var recordFormId = await records.GetRecordFormIdAsync(recordId, cancellationToken);

                if (recordFormId is null)
                {
                    return Results.NotFound(new PrintTemplateErrorResponse("Record was not found."));
                }

                if (recordFormId.Value != version.FormId)
                {
                    return Results.Json(new PrintTemplateErrorResponse("Record does not belong to this print template form."), statusCode: StatusCodes.Status400BadRequest);
                }

                if (!await CanViewFormTemplatesAsync(permissionService, httpContext, version.FormId, cancellationToken))
                {
                    return Results.Forbid();
                }

                var record = await records.GetRecordAsync(httpContext.User, recordId, permissionService, cancellationToken);
                var pdfBytes = pdfService.BuildRecordPdf(version, record);
                await templates.LogPdfGeneratedAsync(
                    version.PrintTemplateId,
                    version.Id,
                    GetCurrentUserId(httpContext),
                    new { recordId },
                    cancellationToken);

                return Results.File(pdfBytes, PrintPdfDocumentBuilder.ContentType, CreatePdfFileName(version.Name, version.VersionNumber));
            });
        });

        versionGroup.MapGet("/{versionId:guid}/reports/{reportId:guid}.pdf", async (
            Guid versionId,
            Guid reportId,
            int? page,
            int? pageSize,
            string? search,
            PrintTemplateService templates,
            PrintPdfService pdfService,
            ReportManagementService reports,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandlePrintTemplateRequestAsync(async () =>
            {
                var version = await templates.GetVersionAsync(versionId, cancellationToken);

                if (!string.Equals(version.Type, PrintTemplateTypes.Report, StringComparison.Ordinal))
                {
                    return Results.Json(new PrintTemplateErrorResponse("Print template version is not a report template."), statusCode: StatusCodes.Status400BadRequest);
                }

                if (version.ReportId != reportId)
                {
                    return Results.Json(new PrintTemplateErrorResponse("Report does not belong to this print template version."), statusCode: StatusCodes.Status400BadRequest);
                }

                if (!await CanViewFormTemplatesAsync(permissionService, httpContext, version.FormId, cancellationToken)
                    || !await permissionService.CanAccessReportAsync(httpContext.User, reportId, PlatformPermissions.Report.View, cancellationToken))
                {
                    return Results.Forbid();
                }

                var report = await reports.ExecuteListReportAsync(
                    httpContext.User,
                    version.FormId,
                    reportId,
                    new RunListReportRequest(page ?? 1, pageSize ?? 100, search),
                    permissionService,
                    cancellationToken);
                var pdfBytes = pdfService.BuildReportPdf(version, report);
                await templates.LogPdfGeneratedAsync(
                    version.PrintTemplateId,
                    version.Id,
                    GetCurrentUserId(httpContext),
                    new { reportId, report.Page, report.PageSize, search },
                    cancellationToken);

                return Results.File(pdfBytes, PrintPdfDocumentBuilder.ContentType, CreatePdfFileName(version.Name, version.VersionNumber));
            });
        });

        return endpoints;
    }

    private static async Task<bool> CanViewFormTemplatesAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.Print, cancellationToken)
            || await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.View, cancellationToken);
    }

    private static async Task<bool> CanManageFormTemplatesAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.Manage, cancellationToken)
            || await permissionService.CanAsync(httpContext.User, PlatformPermissions.Reports.Manage, cancellationToken);
    }

    private static async Task<IResult> HandlePrintTemplateRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (PrintTemplateException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new PrintTemplateErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
        catch (RecordQueryException exception)
        {
            return Results.Json(new PrintTemplateErrorResponse(exception.Message), statusCode: exception.StatusCode);
        }
        catch (ReportManagementException exception)
        {
            return Results.Json(new PrintTemplateErrorResponse(exception.Message), statusCode: exception.StatusCode);
        }
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static string CreatePdfFileName(string name, int versionNumber)
    {
        var safeName = new string(name
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray())
            .Trim('-');

        return $"{(string.IsNullOrWhiteSpace(safeName) ? "print-template" : safeName)}-v{versionNumber}.pdf";
    }
}
