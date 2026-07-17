using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed class LocalizationModule : IPlatformApiModule
{
    public string Id => "enterprise.localization";
    public string Name => "Localization";
    public ModuleOwner Owner => ModuleOwner.Core;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/localization").WithTags("Localization").RequireAuthorization();
        group.MapGet("/current", async (LocalizationService service, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.GetCurrentAsync(WorkspaceMembershipService.GetUserId(context.User), ct)));
        group.MapPut("/workspace", async (SaveWorkspaceLocalizationRequest request, LocalizationService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
        {
            if (!await permissions.CanAsync(context.User, PlatformPermissions.Localization.Manage, ct)) return Results.Forbid();
            return await Handle(() => service.SaveWorkspaceAsync(request, WorkspaceMembershipService.GetUserId(context.User), ct));
        });
        group.MapPut("/me", async (SaveUserLocalizationPreferenceRequest request, LocalizationService service, HttpContext context, CancellationToken ct) =>
            await Handle(() => service.SaveUserAsync(WorkspaceMembershipService.GetUserId(context.User), request, ct)));
    }

    private static async Task<IResult> Handle(Func<Task<LocalizationSettingsDto>> action)
    {
        try { return Results.Ok(await action()); }
        catch (LocalizationException exception) { return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode); }
    }
}
