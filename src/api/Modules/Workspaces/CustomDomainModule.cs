using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed class CustomDomainModule : IPlatformApiModule
{
    public string Id => "enterprise.custom-domains";
    public string Name => "Custom domains";
    public ModuleOwner Owner => ModuleOwner.Core;
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/custom-domains").WithTags("Custom domains").RequireAuthorization();
        group.MapGet("/", async (CustomDomainService service, PermissionService permissions, HttpContext context, CancellationToken ct) => await Allowed(permissions, context, ct) ? Results.Ok(new { items = await service.ListAsync(ct) }) : Results.Forbid());
        group.MapPost("/", async (CreateCustomDomainRequest request, CustomDomainService service, PermissionService permissions, HttpContext context, CancellationToken ct) => !await Allowed(permissions, context, ct) ? Results.Forbid() : await HandleCreated(() => service.CreateAsync(request, WorkspaceMembershipService.GetUserId(context.User), ct)));
        group.MapPost("/{id:guid}/check", async (Guid id, CustomDomainMutationRequest request, CustomDomainService service, PermissionService permissions, HttpContext context, CancellationToken ct) => !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(() => service.CheckAsync(id, request, WorkspaceMembershipService.GetUserId(context.User), ct)));
        group.MapPost("/{id:guid}/enable", async (Guid id, CustomDomainMutationRequest request, CustomDomainService service, PermissionService permissions, HttpContext context, CancellationToken ct) => !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(() => service.EnableAsync(id, request, WorkspaceMembershipService.GetUserId(context.User), ct)));
        group.MapPost("/{id:guid}/disable", async (Guid id, CustomDomainMutationRequest request, CustomDomainService service, PermissionService permissions, HttpContext context, CancellationToken ct) => !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(() => service.DisableAsync(id, request, WorkspaceMembershipService.GetUserId(context.User), ct)));
        group.MapPost("/{id:guid}/rotate", async (Guid id, CustomDomainMutationRequest request, CustomDomainService service, PermissionService permissions, HttpContext context, CancellationToken ct) => !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(() => service.RotateAsync(id, request, WorkspaceMembershipService.GetUserId(context.User), ct)));
    }
    private static Task<bool> Allowed(PermissionService service, HttpContext context, CancellationToken ct) => service.CanAsync(context.User, PlatformPermissions.Domains.Manage, ct);
    private static async Task<IResult> HandleCreated(Func<Task<CustomDomainDto>> action)
    {
        try { var item = await action(); return Results.Created("/api/custom-domains", item); }
        catch (CustomDomainException exception) { return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode); }
    }
    private static async Task<IResult> Handle(Func<Task<CustomDomainDto?>> action)
    {
        try { var item = await action(); return item is null ? Results.NotFound() : Results.Ok(item); }
        catch (CustomDomainException exception) { return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode); }
    }
}
