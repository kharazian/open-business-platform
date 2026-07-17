using System.Text;
using OpenBusinessPlatform.Api.Modules.Workspaces;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class ComplianceModule : IPlatformApiModule
{
    public string Id => "enterprise.compliance";
    public string Name => "Compliance administration";
    public ModuleOwner Owner => ModuleOwner.Core;
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/compliance").WithTags("Compliance").RequireAuthorization();
        group.MapGet("/posture", async (ComplianceService service, PermissionService permissions, HttpContext context, CancellationToken ct) => await Allowed(permissions, context, ct) ? Results.Ok(await service.GetPostureAsync(ct)) : Results.Forbid());
        group.MapGet("/audit", async ([AsParameters] ComplianceAuditQuery query, ComplianceService service, PermissionService permissions, HttpContext context, CancellationToken ct) => !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(() => service.SearchAuditAsync(query, ct)));
        group.MapGet("/audit/export", async ([AsParameters] ComplianceAuditQuery query, ComplianceService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
        {
            if (!await Allowed(permissions, context, ct)) return Results.Forbid();
            try { var file = await service.ExportAuditAsync(query, WorkspaceMembershipService.GetUserId(context.User), ct); return Results.File(Encoding.UTF8.GetBytes(file.Content), file.ContentType, file.FileName); }
            catch (ComplianceException exception) { return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode); }
        });
    }
    private static Task<bool> Allowed(PermissionService service, HttpContext context, CancellationToken ct) => service.CanAsync(context.User, PlatformPermissions.Compliance.Manage, ct);
    private static async Task<IResult> Handle<T>(Func<Task<T>> action) { try { return Results.Ok(await action()); } catch (ComplianceException exception) { return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode); } }
}
