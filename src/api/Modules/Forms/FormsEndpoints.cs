using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Forms;

public static class FormsEndpoints
{
    public static IEndpointRouteBuilder MapFormsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/forms").WithTags("Forms").RequireAuthorization();

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
}
