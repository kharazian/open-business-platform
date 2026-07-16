using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed record AvailableWorkspaceDto(
    Guid MembershipId,
    Guid TenantId,
    string TenantName,
    Guid WorkspaceId,
    string WorkspaceName,
    string WorkspaceSlug,
    string Role,
    string Status,
    bool IsDefault);

public sealed record WorkspaceMembershipDto(
    Guid Id,
    Guid WorkspaceId,
    Guid UserId,
    string UserName,
    string UserEmail,
    string Role,
    string Status,
    bool IsDefault,
    DateTimeOffset InvitedAt,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? SuspendedAt,
    string ConcurrencyStamp);

public sealed record InviteWorkspaceMemberRequest(Guid UserId, string Role);

public sealed record UpdateWorkspaceMembershipRequest(string ConcurrencyStamp);

public sealed record SwitchWorkspaceRequest(Guid WorkspaceId);

public static class WorkspaceMembershipPolicy
{
    public static bool CanTransition(string currentStatus, string nextStatus)
    {
        return (currentStatus, nextStatus) switch
        {
            (WorkspaceMembershipStatuses.Invited, WorkspaceMembershipStatuses.Active) => true,
            (WorkspaceMembershipStatuses.Invited, WorkspaceMembershipStatuses.Suspended) => true,
            (WorkspaceMembershipStatuses.Active, WorkspaceMembershipStatuses.Suspended) => true,
            (WorkspaceMembershipStatuses.Suspended, WorkspaceMembershipStatuses.Invited) => true,
            _ => false
        };
    }
}

public sealed class WorkspaceMembershipException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class WorkspaceMembershipService(
    OpenBusinessPlatformDbContext dbContext,
    IWorkspaceContext workspaceContext)
{
    public async Task<Guid?> ResolveLoginWorkspaceAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await ActiveMemberships(userId)
            .OrderByDescending(membership => membership.IsDefault)
            .ThenBy(membership => membership.CreatedAt)
            .Select(membership => (Guid?)membership.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsActiveMemberAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken)
    {
        return await ActiveMemberships(userId)
            .AnyAsync(membership => membership.WorkspaceId == workspaceId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetRoleNamesAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(userRole => userRole.WorkspaceId == workspaceId && userRole.UserId == userId)
            .Where(userRole => userRole.Role != null
                && userRole.Role.WorkspaceId == workspaceId
                && userRole.Role.IsActive)
            .Select(userRole => userRole.Role!.Name)
            .Distinct()
            .OrderBy(role => role)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AvailableWorkspaceDto>> ListAvailableAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (principal.FindFirstValue(ClaimTypes.NameIdentifier) == BootstrapAdminUserDirectory.BootstrapAdminId)
        {
            return await dbContext.Workspaces
                .AsNoTracking()
                .Where(workspace => workspace.Id == WorkspaceDefaults.WorkspaceId && workspace.IsActive && workspace.Tenant!.IsActive)
                .Select(workspace => new AvailableWorkspaceDto(
                    Guid.Empty,
                    workspace.TenantId,
                    workspace.Tenant!.Name,
                    workspace.Id,
                    workspace.Name,
                    workspace.Slug,
                    WorkspaceMembershipRoles.Owner,
                    WorkspaceMembershipStatuses.Active,
                    true))
                .ToArrayAsync(cancellationToken);
        }

        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Array.Empty<AvailableWorkspaceDto>();
        }

        return await dbContext.WorkspaceMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId.Value)
            .Where(membership => membership.Workspace != null && membership.Workspace.Tenant != null)
            .OrderByDescending(membership => membership.IsDefault)
            .ThenBy(membership => membership.Workspace!.Name)
            .Select(membership => new AvailableWorkspaceDto(
                membership.Id,
                membership.Workspace!.TenantId,
                membership.Workspace.Tenant!.Name,
                membership.WorkspaceId,
                membership.Workspace.Name,
                membership.Workspace.Slug,
                membership.Role,
                membership.Status,
                membership.IsDefault))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkspaceMembershipDto>> ListCurrentAsync(CancellationToken cancellationToken)
    {
        var memberships = await dbContext.WorkspaceMemberships
            .AsNoTracking()
            .Include(membership => membership.User)
            .Where(membership => membership.WorkspaceId == workspaceContext.WorkspaceId)
            .OrderBy(membership => membership.User!.Name)
            .ToArrayAsync(cancellationToken);

        return memberships.Select(ToDto).ToArray();
    }

    public async Task<WorkspaceMembershipDto> InviteAsync(
        InviteWorkspaceMemberRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var role = NormalizeRole(request.Role);
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == request.UserId && candidate.IsActive, cancellationToken)
            ?? throw new WorkspaceMembershipException(StatusCodes.Status400BadRequest, "An active user is required.");

        var membership = await dbContext.WorkspaceMemberships
            .SingleOrDefaultAsync(candidate =>
                candidate.WorkspaceId == workspaceContext.WorkspaceId
                && candidate.UserId == request.UserId,
                cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (membership is null)
        {
            membership = new WorkspaceMembership
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceContext.WorkspaceId,
                UserId = user.Id,
                Role = role,
                Status = WorkspaceMembershipStatuses.Invited,
                InvitedById = actorUserId,
                InvitedAt = now
            };
            dbContext.WorkspaceMemberships.Add(membership);
        }
        else
        {
            EnsureTransition(membership.Status, WorkspaceMembershipStatuses.Invited);
            membership.Role = role;
            membership.Status = WorkspaceMembershipStatuses.Invited;
            membership.IsDefault = false;
            membership.InvitedById = actorUserId;
            membership.InvitedAt = now;
            membership.ActivatedAt = null;
            membership.SuspendedAt = null;
        }

        AddAudit(membership.Id, "workspace_membership_invited", actorUserId, membership.UserId, role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(membership, user.Name, user.Email);
    }

    public async Task<WorkspaceMembershipDto?> ActivateAsync(
        Guid membershipId,
        UpdateWorkspaceMembershipRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentMembershipQuery().SingleOrDefaultAsync(item => item.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return null;
        }

        EnsureStamp(membership, request.ConcurrencyStamp);
        EnsureTransition(membership.Status, WorkspaceMembershipStatuses.Active);
        membership.Status = WorkspaceMembershipStatuses.Active;
        membership.ActivatedAt = DateTimeOffset.UtcNow;
        membership.SuspendedAt = null;
        membership.IsDefault = !await dbContext.WorkspaceMemberships.AnyAsync(candidate =>
            candidate.UserId == membership.UserId
            && candidate.Status == WorkspaceMembershipStatuses.Active
            && candidate.IsDefault,
            cancellationToken);
        AddAudit(membership.Id, "workspace_membership_activated", actorUserId, membership.UserId, membership.Role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(membership);
    }

    public async Task<WorkspaceMembershipDto?> SuspendAsync(
        Guid membershipId,
        UpdateWorkspaceMembershipRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentMembershipQuery().SingleOrDefaultAsync(item => item.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return null;
        }

        if (actorUserId == membership.UserId)
        {
            throw new WorkspaceMembershipException(StatusCodes.Status400BadRequest, "You cannot suspend your own workspace membership.");
        }

        EnsureStamp(membership, request.ConcurrencyStamp);
        EnsureTransition(membership.Status, WorkspaceMembershipStatuses.Suspended);
        membership.Status = WorkspaceMembershipStatuses.Suspended;
        membership.IsDefault = false;
        membership.SuspendedAt = DateTimeOffset.UtcNow;
        AddAudit(membership.Id, "workspace_membership_suspended", actorUserId, membership.UserId, membership.Role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(membership);
    }

    private IQueryable<WorkspaceMembership> ActiveMemberships(Guid userId)
    {
        return dbContext.WorkspaceMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && membership.Status == WorkspaceMembershipStatuses.Active)
            .Where(membership => membership.User != null
                && membership.User.IsActive
                && membership.Workspace != null
                && membership.Workspace.IsActive
                && membership.Workspace.Tenant != null
                && membership.Workspace.Tenant.IsActive);
    }

    private IQueryable<WorkspaceMembership> CurrentMembershipQuery()
    {
        return dbContext.WorkspaceMemberships
            .Include(membership => membership.User)
            .Where(membership => membership.WorkspaceId == workspaceContext.WorkspaceId);
    }

    private static WorkspaceMembershipDto ToDto(WorkspaceMembership membership)
    {
        return ToDto(membership, membership.User!.Name, membership.User.Email);
    }

    private static WorkspaceMembershipDto ToDto(WorkspaceMembership membership, string userName, string userEmail)
    {
        return new WorkspaceMembershipDto(
            membership.Id,
            membership.WorkspaceId,
            membership.UserId,
            userName,
            userEmail,
            membership.Role,
            membership.Status,
            membership.IsDefault,
            membership.InvitedAt,
            membership.ActivatedAt,
            membership.SuspendedAt,
            membership.ConcurrencyStamp);
    }

    private void AddAudit(Guid membershipId, string action, Guid? actorUserId, Guid userId, string role)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "WorkspaceMembership",
            EntityId = membershipId,
            Action = action,
            UserId = actorUserId,
            MetadataJson = JsonSerializer.SerializeToDocument(new { userId, role })
        });
    }

    private static string NormalizeRole(string role)
    {
        var normalized = role?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!WorkspaceMembershipRoles.Supported.Contains(normalized))
        {
            throw new WorkspaceMembershipException(StatusCodes.Status400BadRequest, "Workspace membership role is invalid.");
        }

        return normalized;
    }

    private static void EnsureTransition(string currentStatus, string nextStatus)
    {
        if (!WorkspaceMembershipPolicy.CanTransition(currentStatus, nextStatus))
        {
            throw new WorkspaceMembershipException(StatusCodes.Status400BadRequest, $"Cannot change membership from {currentStatus} to {nextStatus}.");
        }
    }

    private static void EnsureStamp(WorkspaceMembership membership, string stamp)
    {
        if (string.IsNullOrWhiteSpace(stamp) || !string.Equals(membership.ConcurrencyStamp, stamp.Trim(), StringComparison.Ordinal))
        {
            throw new DbUpdateConcurrencyException("The workspace membership changed. Refresh and try again.");
        }
    }

    public static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
