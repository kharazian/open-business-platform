using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Forms;

public static class FormsEndpoints
{
    public static IEndpointRouteBuilder MapFormsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/forms").WithTags("Forms").RequireAuthorization();

        group.MapGet("", async (
            FormManagementService formManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanListFormsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await formManagement.ListFormsAsync(cancellationToken) });
        });

        group.MapGet("/{formId:guid}", async (
            Guid formId,
            FormManagementService formManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageFormAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleFormRequestAsync(async () =>
            {
                var form = await formManagement.GetFormAsync(formId, cancellationToken);
                return Results.Ok(form);
            });
        });

        group.MapPost("", async (
            CreateFormRequest request,
            FormManagementService formManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanCreateFormsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleFormRequestAsync(async () =>
            {
                var form = await formManagement.CreateFormAsync(request, cancellationToken);
                return Results.Created($"/api/forms/{form.Id}", form);
            });
        });

        group.MapPut("/{formId:guid}", async (
            Guid formId,
            UpdateFormDraftRequest request,
            FormManagementService formManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageFormAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleFormRequestAsync(async () =>
            {
                var form = await formManagement.UpdateDraftAsync(formId, request, cancellationToken);
                return Results.Ok(form);
            });
        });

        group.MapPost("/{formId:guid}/publish", async (
            Guid formId,
            FormManagementService formManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageFormAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleFormRequestAsync(async () =>
            {
                var response = await formManagement.PublishFormAsync(formId, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Ok(response);
            });
        });

        group.MapGet("/access-options", async (
            OpenBusinessPlatformDbContext dbContext,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var canManageRoles = await permissionService.CanAsync(httpContext.User, PlatformPermissions.Roles.Manage, cancellationToken);
            var canManageForms = await permissionService.CanAsync(httpContext.User, PlatformPermissions.Forms.ManageAll, cancellationToken);

            if (!canManageRoles && !canManageForms)
            {
                return Results.Forbid();
            }

            var forms = await dbContext.Forms
                .AsNoTracking()
                .Where(form => !form.IsDeleted)
                .OrderBy(form => form.Name)
                .Select(form => new FormAccessOptionDto(form.Id, form.Name, form.Status))
                .ToArrayAsync(cancellationToken);

            return Results.Ok(new { items = forms });
        });

        return endpoints;
    }

    private static async Task<bool> CanListFormsAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Forms, cancellationToken)
            || await permissionService.CanAsync(httpContext.User, PlatformPermissions.Forms.Create, cancellationToken)
            || await permissionService.CanAsync(httpContext.User, PlatformPermissions.Forms.ManageAll, cancellationToken);
    }

    private static async Task<bool> CanCreateFormsAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Forms.Create, cancellationToken)
            || await permissionService.CanAsync(httpContext.User, PlatformPermissions.Forms.ManageAll, cancellationToken);
    }

    private static async Task<bool> CanManageFormAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.Manage, cancellationToken);
    }

    private static async Task<IResult> HandleFormRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (FormManagementException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new FormErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
