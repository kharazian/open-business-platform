using OpenBusinessPlatform.Api.Modules.Workspaces;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class RetentionModule : IPlatformApiModule
{
    public string Id => "enterprise.retention";
    public string Name => "Retention";
    public ModuleOwner Owner => ModuleOwner.Core;
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/retention").WithTags("Retention").RequireAuthorization();
        group.MapGet("/policies", async (RetentionService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            await Allowed(permissions, context, ct) ? Results.Ok(new { items = await service.ListPoliciesAsync(ct) }) : Results.Forbid());
        group.MapPost("/policies", async (SaveRetentionPolicyRequest request, RetentionService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Created("/api/retention/policies", await service.CreatePolicyAsync(request, WorkspaceMembershipService.GetUserId(context.User), ct))));
        group.MapPut("/policies/{id:guid}", async (Guid id, SaveRetentionPolicyRequest request, RetentionService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => { var item = await service.UpdatePolicyAsync(id, request, WorkspaceMembershipService.GetUserId(context.User), ct); return item is null ? Results.NotFound() : Results.Ok(item); }));
        group.MapPost("/policies/{id:guid}/dry-run", async (Guid id, RetentionService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => { var result = await service.DryRunAsync(id, WorkspaceMembershipService.GetUserId(context.User), ct); return result is null ? Results.NotFound() : Results.Ok(result); }));
        group.MapGet("/legal-holds", async (RetentionService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            await Allowed(permissions, context, ct) ? Results.Ok(new { items = await service.ListHoldsAsync(ct) }) : Results.Forbid());
        group.MapPost("/legal-holds", async (PlaceLegalHoldRequest request, RetentionService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Created("/api/retention/legal-holds", await service.PlaceHoldAsync(request, WorkspaceMembershipService.GetUserId(context.User), ct))));
        group.MapPost("/legal-holds/{id:guid}/release", async (Guid id, ReleaseLegalHoldRequest request, RetentionService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => { var item = await service.ReleaseHoldAsync(id, request, WorkspaceMembershipService.GetUserId(context.User), ct); return item is null ? Results.NotFound() : Results.Ok(item); }));
    }
    private static Task<bool> Allowed(PermissionService permissions, HttpContext context, CancellationToken ct) => permissions.CanAsync(context.User, PlatformPermissions.Retention.Manage, ct);
    private static async Task<IResult> Handle(Func<Task<IResult>> action) { try { return await action(); } catch (RetentionException ex) { return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode); } }
}
