using System.Text;
using OpenBusinessPlatform.Api.Modules.Workspaces;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class AdministrativeBackupModule : IPlatformApiModule
{
    public string Id => "enterprise.backups";
    public string Name => "Administrative backups";
    public ModuleOwner Owner => ModuleOwner.Core;
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/administration/backups").WithTags("Administrative backups").RequireAuthorization();
        group.MapGet("/", async (AdministrativeBackupService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            await Allowed(permissions, context, ct) ? Results.Ok(new { items = await service.ListAsync(ct) }) : Results.Forbid());
        group.MapGet("/{id:guid}", async (Guid id, AdministrativeBackupService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => { var item = await service.GetAsync(id, ct); return item is null ? Results.NotFound() : Results.Ok(item); }));
        group.MapPost("/", async (CreateAdministrativeBackupRequest request, AdministrativeBackupService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Created("/api/administration/backups", await service.CreateAsync(context.User, request, WorkspaceMembershipService.GetUserId(context.User), ct))));
        group.MapGet("/{id:guid}/artifact", async (Guid id, AdministrativeBackupService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => { var file = await service.DownloadAsync(id, WorkspaceMembershipService.GetUserId(context.User), ct); return file is null ? Results.NotFound() : Results.File(Encoding.UTF8.GetBytes(file.Content), file.ContentType, file.FileName); }));
        group.MapPost("/{id:guid}/restore-plan", async (Guid id, AdministrativeBackupService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => { var plan = await service.PlanRestoreAsync(id, WorkspaceMembershipService.GetUserId(context.User), ct); return plan is null ? Results.NotFound() : Results.Ok(plan); }));
    }
    private static Task<bool> Allowed(PermissionService service, HttpContext context, CancellationToken ct) => service.CanAsync(context.User, PlatformPermissions.Backup.Manage, ct);
    private static async Task<IResult> Handle(Func<Task<IResult>> action) { try { return await action(); } catch (AdministrativeBackupException ex) { return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode); } }
}
