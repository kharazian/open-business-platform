using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Common;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public interface IWorkspaceContext
{
    Guid WorkspaceId { get; }
}

public static class WorkspaceClaims
{
    public const string WorkspaceId = "obp.workspace_id";
}

public sealed class DefaultWorkspaceContext : IWorkspaceContext
{
    public Guid WorkspaceId => WorkspaceDefaults.WorkspaceId;
}

public sealed class HttpContextWorkspaceContext(IHttpContextAccessor httpContextAccessor) : IWorkspaceContext
{
    public const string ResolvedDomainWorkspaceItemKey = "obp.resolved_domain_workspace";

    public Guid WorkspaceId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            return context?.Items[ResolvedDomainWorkspaceItemKey] is Guid domainWorkspaceId && domainWorkspaceId != Guid.Empty
                ? domainWorkspaceId
                : ResolveWorkspaceId(context?.User);
        }
    }

    public static Guid ResolveWorkspaceId(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirstValue(WorkspaceClaims.WorkspaceId);
        return Guid.TryParse(value, out var workspaceId) && workspaceId != Guid.Empty
            ? workspaceId
            : WorkspaceDefaults.WorkspaceId;
    }
}

public static class WorkspaceOwnershipGuard
{
    public static void AssignForCreate(IWorkspaceOwned entity, Guid activeWorkspaceId)
    {
        if (entity.WorkspaceId == Guid.Empty)
        {
            entity.WorkspaceId = activeWorkspaceId;
        }

        EnsureActive(entity, activeWorkspaceId);
    }

    public static void EnsureActive(IWorkspaceOwned entity, Guid activeWorkspaceId)
    {
        if (entity.WorkspaceId != activeWorkspaceId)
        {
            throw new InvalidOperationException("Cannot write data outside the active workspace.");
        }
    }
}

public sealed record CurrentWorkspaceDto(
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    Guid WorkspaceId,
    string WorkspaceName,
    string WorkspaceSlug,
    bool IsDefault);

public sealed class WorkspaceContextService(
    OpenBusinessPlatformDbContext dbContext,
    IWorkspaceContext workspaceContext)
{
    public async Task<CurrentWorkspaceDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Workspaces
            .AsNoTracking()
            .Where(workspace =>
                workspace.Id == workspaceContext.WorkspaceId
                && workspace.IsActive
                && workspace.Tenant!.IsActive)
            .Select(workspace => new CurrentWorkspaceDto(
                workspace.TenantId,
                workspace.Tenant!.Name,
                workspace.Tenant.Slug,
                workspace.Id,
                workspace.Name,
                workspace.Slug,
                workspace.IsDefault))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
