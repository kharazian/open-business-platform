using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed record FieldAccessResult(IReadOnlySet<string> HiddenFieldIds, IReadOnlySet<string> ReadOnlyFieldIds);

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

    public async Task<IReadOnlyCollection<string>> GetAllowedRecordScopesAsync(
        ClaimsPrincipal principal,
        Guid formId,
        string action,
        CancellationToken cancellationToken)
    {
        if (formId == Guid.Empty || !PlatformPermissions.FormActions.Contains(action))
        {
            return Array.Empty<string>();
        }

        if (HasAllAccess(principal) || await CanAsync(principal, PlatformPermissions.Forms.ManageAll, cancellationToken))
        {
            return new[] { PlatformPermissions.RecordScopes.All };
        }

        var userId = GetLocalUserId(principal);

        if (userId is null)
        {
            return Array.Empty<string>();
        }

        var scopedPermissions = await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId.Value)
            .Where(userRole => userRole.Role != null && userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role!.FormPermissions)
            .Where(permission => permission.FormId == formId)
            .Where(permission => permission.Action == action || permission.Action == PlatformPermissions.Form.Manage)
            .Select(permission => new { permission.Action, permission.Scope })
            .ToArrayAsync(cancellationToken);

        if (scopedPermissions.Any(permission => permission.Action == PlatformPermissions.Form.Manage))
        {
            return new[] { PlatformPermissions.RecordScopes.All };
        }

        return scopedPermissions
            .Select(permission => NormalizeScope(permission.Scope))
            .Where(PlatformPermissions.RecordScopes.Supported.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<IQueryable<FormRecord>> ApplyRecordAccessAsync(
        ClaimsPrincipal principal,
        IQueryable<FormRecord> records,
        Guid formId,
        string action,
        CancellationToken cancellationToken)
    {
        var scopes = await GetAllowedRecordScopesAsync(principal, formId, action, cancellationToken);

        if (scopes.Count == 0)
        {
            return records.Where(record => false);
        }

        var context = await GetRecordAccessContextAsync(principal, cancellationToken);
        return RecordAccessEvaluator.Apply(records, context, scopes);
    }

    public async Task<bool> CanAccessRecordAsync(
        ClaimsPrincipal principal,
        FormRecord record,
        string action,
        CancellationToken cancellationToken)
    {
        var scopes = await GetAllowedRecordScopesAsync(principal, record.FormId, action, cancellationToken);

        if (scopes.Count == 0)
        {
            return false;
        }

        if (scopes.Contains(PlatformPermissions.RecordScopes.All))
        {
            return true;
        }

        var context = await GetRecordAccessContextAsync(principal, cancellationToken);
        return RecordAccessEvaluator.CanAccess(record, context, scopes);
    }

    public async Task<FieldAccessResult> GetFieldAccessAsync(
        ClaimsPrincipal principal,
        Guid formId,
        CancellationToken cancellationToken)
    {
        if (formId == Guid.Empty
            || HasAllAccess(principal)
            || await CanAsync(principal, PlatformPermissions.Forms.ManageAll, cancellationToken))
        {
            return new FieldAccessResult(new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));
        }

        var userId = GetLocalUserId(principal);

        if (userId is null)
        {
            return new FieldAccessResult(new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));
        }

        var fieldPermissions = await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId.Value)
            .Where(userRole => userRole.Role != null && userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role!.FieldPermissions)
            .Where(permission => permission.FormId == formId)
            .Select(permission => new { permission.FieldId, permission.Access })
            .ToArrayAsync(cancellationToken);

        var hidden = fieldPermissions
            .Where(permission => permission.Access == PlatformPermissions.FieldAccess.Hidden)
            .Select(permission => permission.FieldId)
            .ToHashSet(StringComparer.Ordinal);
        var readOnly = fieldPermissions
            .Where(permission => permission.Access == PlatformPermissions.FieldAccess.ReadOnly && !hidden.Contains(permission.FieldId))
            .Select(permission => permission.FieldId)
            .ToHashSet(StringComparer.Ordinal);

        return new FieldAccessResult(hidden, readOnly);
    }

    public async Task<bool> CanAccessReportAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        string action,
        CancellationToken cancellationToken)
    {
        if (reportId == Guid.Empty || !PlatformPermissions.ReportActions.Contains(action))
        {
            return false;
        }

        if (HasAllAccess(principal) || await CanAsync(principal, PlatformPermissions.Reports.Manage, cancellationToken))
        {
            return true;
        }

        var report = await dbContext.Reports
            .AsNoTracking()
            .Where(candidate => candidate.Id == reportId && !candidate.IsDeleted)
            .Select(candidate => new { candidate.Id, candidate.FormId })
            .FirstOrDefaultAsync(cancellationToken);

        if (report is null)
        {
            return false;
        }

        var hasExplicitReportPermissions = await dbContext.RoleReportPermissions
            .AsNoTracking()
            .AnyAsync(permission => permission.ReportId == reportId, cancellationToken);

        if (!hasExplicitReportPermissions)
        {
            return action == PlatformPermissions.Report.Manage
                ? await CanAccessFormAsync(principal, report.FormId, PlatformPermissions.Form.Manage, cancellationToken)
                : await CanAccessFormAsync(principal, report.FormId, PlatformPermissions.Form.View, cancellationToken);
        }

        var userId = GetLocalUserId(principal);

        if (userId is null)
        {
            return false;
        }

        var allowedActions = action == PlatformPermissions.Report.Manage
            ? new[] { PlatformPermissions.Report.Manage }
            : new[] { action, PlatformPermissions.Report.Manage };

        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId.Value)
            .Where(userRole => userRole.Role != null && userRole.Role.IsActive)
            .AnyAsync(
                userRole => userRole.Role!.ReportPermissions.Any(permission =>
                    permission.ReportId == reportId && allowedActions.Contains(permission.Action)),
                cancellationToken);
    }

    private async Task<RecordAccessContext> GetRecordAccessContextAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetLocalUserId(principal);

        if (userId is null)
        {
            return new RecordAccessContext(null, Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<Guid>());
        }

        var departmentIds = await dbContext.UserDepartments
            .AsNoTracking()
            .Where(userDepartment => userDepartment.UserId == userId.Value)
            .Select(userDepartment => userDepartment.DepartmentId)
            .ToArrayAsync(cancellationToken);
        var managedDepartmentIds = await dbContext.Departments
            .AsNoTracking()
            .Where(department => department.ManagerUserId == userId.Value && department.IsActive)
            .Select(department => department.Id)
            .ToArrayAsync(cancellationToken);
        var groupIds = await dbContext.UserGroups
            .AsNoTracking()
            .Where(userGroup => userGroup.UserId == userId.Value)
            .Select(userGroup => userGroup.GroupId)
            .ToArrayAsync(cancellationToken);

        return new RecordAccessContext(userId, departmentIds, managedDepartmentIds, groupIds);
    }

    private static string NormalizeScope(string? scope)
    {
        return string.IsNullOrWhiteSpace(scope) ? PlatformPermissions.RecordScopes.All : scope.Trim();
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
