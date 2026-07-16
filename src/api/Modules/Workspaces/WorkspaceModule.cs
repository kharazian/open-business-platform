using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed class WorkspaceModule : IPlatformApiModule
{
    public string Id => "enterprise.workspaces";

    public string Name => "Workspaces";

    public ModuleOwner Owner => ModuleOwner.Core;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/workspaces/current", async (
            WorkspaceContextService workspaceContextService,
            CancellationToken cancellationToken) =>
        {
            var current = await workspaceContextService.GetCurrentAsync(cancellationToken);
            return current is null ? Results.NotFound() : Results.Ok(current);
        })
        .WithTags("Workspaces")
        .RequireAuthorization();

        endpoints.MapGet("/api/workspaces/available", async (
            WorkspaceMembershipService memberships,
            HttpContext httpContext,
            CancellationToken cancellationToken) => Results.Ok(new
            {
                items = await memberships.ListAvailableAsync(httpContext.User, cancellationToken)
            }))
            .WithTags("Workspaces")
            .RequireAuthorization();

        endpoints.MapPost("/api/workspaces/current", async (
            SwitchWorkspaceRequest request,
            WorkspaceMembershipService memberships,
            WorkspaceContextService workspaceContextService,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!string.Equals(httpContext.User.Identity?.AuthenticationType, CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal))
            {
                return Results.Forbid();
            }

            var subject = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var localUserId = WorkspaceMembershipService.GetUserId(httpContext.User);
            var allowed = subject == BootstrapAdminUserDirectory.BootstrapAdminId
                ? request.WorkspaceId == WorkspaceDefaults.WorkspaceId
                : localUserId is not null
                    && await memberships.IsActiveMemberAsync(localUserId.Value, request.WorkspaceId, cancellationToken);

            if (!allowed)
            {
                return Results.Forbid();
            }

            var name = httpContext.User.FindFirstValue(ClaimTypes.Name);
            var email = httpContext.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                return Results.Unauthorized();
            }

            var roles = localUserId is null
                ? httpContext.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
                : await memberships.GetRoleNamesAsync(localUserId.Value, request.WorkspaceId, cancellationToken);
            var user = new AuthenticatedUser(subject, name, email, roles);
            var principal = IdentityPrincipalFactory.Create(user, request.WorkspaceId);
            httpContext.User = principal;
            var permissions = await permissionService.GetEffectivePermissionsAsync(principal, cancellationToken);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow, IsPersistent = false });

            var current = await workspaceContextService.GetCurrentAsync(cancellationToken);
            return current is null
                ? Results.NotFound()
                : Results.Ok(new
                {
                    workspace = current,
                    user = (user with { Permissions = permissions }).ToResponse(request.WorkspaceId)
                });
        })
        .WithTags("Workspaces")
        .RequireAuthorization();

        var membershipEndpoints = endpoints.MapGroup("/api/workspaces/memberships")
            .WithTags("Workspace memberships")
            .RequireAuthorization();

        membershipEndpoints.MapGet("/", async (
            WorkspaceMembershipService memberships,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await memberships.ListCurrentAsync(cancellationToken) });
        });

        membershipEndpoints.MapPost("/", async (
            InviteWorkspaceMemberRequest request,
            WorkspaceMembershipService memberships,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleMembershipRequestAsync(async () => Results.Created(
                "/api/workspaces/memberships",
                await memberships.InviteAsync(request, WorkspaceMembershipService.GetUserId(httpContext.User), cancellationToken)));
        });

        membershipEndpoints.MapPost("/{membershipId:guid}/activate", async (
            Guid membershipId,
            UpdateWorkspaceMembershipRequest request,
            WorkspaceMembershipService memberships,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleMembershipRequestAsync(async () =>
            {
                var membership = await memberships.ActivateAsync(
                    membershipId,
                    request,
                    WorkspaceMembershipService.GetUserId(httpContext.User),
                    cancellationToken);
                return membership is null ? Results.NotFound() : Results.Ok(membership);
            });
        });

        membershipEndpoints.MapPost("/{membershipId:guid}/suspend", async (
            Guid membershipId,
            UpdateWorkspaceMembershipRequest request,
            WorkspaceMembershipService memberships,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleMembershipRequestAsync(async () =>
            {
                var membership = await memberships.SuspendAsync(
                    membershipId,
                    request,
                    WorkspaceMembershipService.GetUserId(httpContext.User),
                    cancellationToken);
                return membership is null ? Results.NotFound() : Results.Ok(membership);
            });
        });
    }

    private static async Task<IResult> HandleMembershipRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (WorkspaceMembershipException exception)
        {
            return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }
    }
}
