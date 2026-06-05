using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

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
                return Results.Ok(new { items });
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

                return Results.Ok(template);
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

                await templates.DeleteAsync(templateId, GetCurrentUserId(httpContext), cancellationToken);
                return Results.NoContent();
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
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
