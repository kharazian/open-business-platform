using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed class WorkspaceBrandingModule : IPlatformApiModule
{
    public string Id => "enterprise.workspace-branding";
    public string Name => "Workspace branding";
    public ModuleOwner Owner => ModuleOwner.Core;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/branding/public", async (string? tenant, string? workspace, WorkspaceBrandingService service, CancellationToken ct) =>
        {
            var branding = await service.GetPublicAsync(tenant, workspace, ct);
            return branding is null ? Results.NotFound() : Results.Ok(branding);
        }).WithTags("Workspace branding").AllowAnonymous();

        var group = endpoints.MapGroup("/api/branding/current").WithTags("Workspace branding").RequireAuthorization();
        group.MapGet("/", async (WorkspaceBrandingService service, CancellationToken ct) => Results.Ok(await service.GetCurrentAsync(ct)));
        group.MapPut("/", async (SaveWorkspaceBrandingRequest request, WorkspaceBrandingService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
        {
            if (!await permissions.CanAsync(context.User, PlatformPermissions.Branding.Manage, ct)) return Results.Forbid();
            try
            {
                return Results.Ok(await service.SaveCurrentAsync(request, WorkspaceMembershipService.GetUserId(context.User), ct));
            }
            catch (WorkspaceBrandingException exception)
            {
                return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode);
            }
        });
    }
}
