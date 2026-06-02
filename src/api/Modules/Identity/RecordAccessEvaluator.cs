using OpenBusinessPlatform.Api.Domain.Entities;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed record RecordAccessContext(
    Guid? UserId,
    IReadOnlyCollection<Guid> DepartmentIds,
    IReadOnlyCollection<Guid> ManagedDepartmentIds,
    IReadOnlyCollection<Guid> GroupIds);

public static class RecordAccessEvaluator
{
    public static IQueryable<FormRecord> Apply(
        IQueryable<FormRecord> records,
        RecordAccessContext context,
        IReadOnlyCollection<string> scopes)
    {
        var normalized = scopes.Distinct(StringComparer.Ordinal).ToArray();

        if (normalized.Contains(PlatformPermissions.RecordScopes.All))
        {
            return records;
        }

        return records.Where(record =>
            normalized.Contains(PlatformPermissions.RecordScopes.Own)
                && context.UserId != null
                && (record.OwnerId == context.UserId || record.CreatedById == context.UserId)
            || normalized.Contains(PlatformPermissions.RecordScopes.Department)
                && record.DepartmentId != null
                && context.DepartmentIds.Contains(record.DepartmentId.Value)
            || normalized.Contains(PlatformPermissions.RecordScopes.ManagedDepartment)
                && record.DepartmentId != null
                && context.ManagedDepartmentIds.Contains(record.DepartmentId.Value)
            || normalized.Contains(PlatformPermissions.RecordScopes.Group)
                && record.AssignedGroupId != null
                && context.GroupIds.Contains(record.AssignedGroupId.Value)
            || normalized.Contains(PlatformPermissions.RecordScopes.Assigned)
                && context.UserId != null
                && record.AssignedToUserId == context.UserId);
    }

    public static bool CanAccess(
        FormRecord record,
        RecordAccessContext context,
        IReadOnlyCollection<string> scopes)
    {
        return Apply(new[] { record }.AsQueryable(), context, scopes).Any();
    }
}
