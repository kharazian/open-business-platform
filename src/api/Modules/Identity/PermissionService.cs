using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class PermissionService
{
    private readonly OpenBusinessPlatformDbContext dbContext;

    public PermissionService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (HasAllAccess(principal))
        {
            return PlatformPermissions.AllBuiltInPermissions;
        }

        var userId = GetLocalUserId(principal);

        if (userId is null)
        {
            return Array.Empty<string>();
        }

        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId.Value)
            .Where(userRole => userRole.Role != null && userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role!.Permissions.Select(permission => permission.Permission))
            .Distinct()
            .OrderBy(permission => permission)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> CanAsync(
        ClaimsPrincipal principal,
        string permission,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        if (HasAllAccess(principal))
        {
            return true;
        }

        var userId = GetLocalUserId(principal);

        if (userId is null)
        {
            return false;
        }

        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId.Value)
            .Where(userRole => userRole.Role != null && userRole.Role.IsActive)
            .AnyAsync(
                userRole => userRole.Role!.Permissions.Any(rolePermission => rolePermission.Permission == permission),
                cancellationToken);
    }

    public async Task<bool> CanAccessFormAsync(
        ClaimsPrincipal principal,
        Guid formId,
        string action,
        CancellationToken cancellationToken)
    {
        if (formId == Guid.Empty || !PlatformPermissions.FormActions.Contains(action))
        {
            return false;
        }

        if (HasAllAccess(principal) || await CanAsync(principal, PlatformPermissions.Forms.ManageAll, cancellationToken))
        {
            return true;
        }

        var userId = GetLocalUserId(principal);

        if (userId is null)
        {
            return false;
        }

        var allowedActions = action == PlatformPermissions.Form.Manage
            ? new[] { PlatformPermissions.Form.Manage }
            : new[] { action, PlatformPermissions.Form.Manage };

        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId.Value)
            .Where(userRole => userRole.Role != null && userRole.Role.IsActive)
            .AnyAsync(
                userRole => userRole.Role!.FormPermissions.Any(formPermission =>
                    formPermission.FormId == formId
                    && allowedActions.Contains(formPermission.Action)),
                cancellationToken);
    }

    private static bool HasAllAccess(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier) == BootstrapAdminUserDirectory.BootstrapAdminId
            || principal.IsInRole(PlatformRoles.Admin);
    }

    private static Guid? GetLocalUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
