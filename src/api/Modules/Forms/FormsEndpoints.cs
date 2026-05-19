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

    private static async Task<IResult> HandleFormRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (FormManagementException exception)
        {
            return Results.Json(new FormErrorResponse(exception.Message), statusCode: exception.StatusCode);
        }
    }
}
