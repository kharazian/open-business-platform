using OpenBusinessPlatform.Api.Modules.Workspaces;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class AccessPolicyModule : IPlatformApiModule
{
    public string Id => "enterprise.access-policies";
    public string Name => "Access policies";
    public ModuleOwner Owner => ModuleOwner.Core;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/access-policies")
            .WithTags("Access policies")
            .RequireAuthorization();

        group.MapGet("/", async (
            AccessPolicyService policies,
            PermissionService permissions,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (!await permissions.CanAsync(context.User, PlatformPermissions.Roles.Manage, cancellationToken)) return Results.Forbid();
            return Results.Ok(new { items = await policies.ListAsync(cancellationToken) });
        });

        group.MapPost("/", async (
            SaveAccessPolicyRequest request,
            AccessPolicyService policies,
            PermissionService permissions,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (!await permissions.CanAsync(context.User, PlatformPermissions.Roles.Manage, cancellationToken)) return Results.Forbid();
            return await HandleAsync(async () => Results.Created(
                "/api/access-policies",
                await policies.CreateAsync(request, WorkspaceMembershipService.GetUserId(context.User), cancellationToken)));
        });

        group.MapPut("/{policyId:guid}", async (
            Guid policyId,
            SaveAccessPolicyRequest request,
            AccessPolicyService policies,
            PermissionService permissions,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (!await permissions.CanAsync(context.User, PlatformPermissions.Roles.Manage, cancellationToken)) return Results.Forbid();
            return await HandleAsync(async () =>
            {
                var policy = await policies.UpdateAsync(
                    policyId, request, WorkspaceMembershipService.GetUserId(context.User), cancellationToken);
                return policy is null ? Results.NotFound() : Results.Ok(policy);
            });
        });

        group.MapPost("/simulate", async (
            SimulateAccessPolicyRequest request,
            AccessPolicyService policies,
            PermissionService permissions,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (!await permissions.CanAsync(context.User, PlatformPermissions.Roles.Manage, cancellationToken)) return Results.Forbid();
            return await HandleAsync(async () => Results.Ok(await policies.SimulateAsync(request, cancellationToken)));
        });
    }

    private static async Task<IResult> HandleAsync(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (AccessPolicyException exception)
        {
            return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode);
        }
    }
}
