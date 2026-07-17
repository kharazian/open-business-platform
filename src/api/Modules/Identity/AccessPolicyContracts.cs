namespace OpenBusinessPlatform.Api.Modules.Identity;

public static class AccessPolicyResourceTypes
{
    public const string Platform = "platform";
    public const string Form = "form";
    public const string Report = "report";
    public const string Record = "record";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Platform, Form, Report, Record
    };
}

public static class AccessPolicyActions
{
    public static IReadOnlySet<string> Record { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        PlatformPermissions.Form.View,
        PlatformPermissions.Form.Edit,
        PlatformPermissions.Form.Delete,
        PlatformPermissions.Form.Print,
        PlatformPermissions.Form.Export,
        PlatformPermissions.Form.Assign,
        PlatformPermissions.Form.ChangeStatus
    };
}

public sealed record AccessPolicyConditions(
    IReadOnlyCollection<string>? RoleAny = null,
    IReadOnlyCollection<string>? MembershipRoleAny = null,
    IReadOnlyCollection<Guid>? DepartmentAny = null,
    IReadOnlyCollection<Guid>? GroupAny = null,
    IReadOnlyCollection<string>? RecordStatusAny = null,
    bool? RecordOwnerIsCurrentUser = null);

public sealed record AccessPolicyDto(
    Guid Id,
    string Name,
    string? Description,
    string ResourceType,
    Guid? ResourceId,
    string Action,
    AccessPolicyConditions Conditions,
    int Priority,
    bool IsEnabled,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record SaveAccessPolicyRequest(
    string Name,
    string? Description,
    string ResourceType,
    Guid? ResourceId,
    string Action,
    AccessPolicyConditions Conditions,
    int Priority,
    bool IsEnabled,
    string? ConcurrencyStamp);

public sealed record SimulateAccessPolicyRequest(
    Guid UserId,
    string ResourceType,
    Guid? ResourceId,
    string Action,
    string? RecordStatus,
    Guid? RecordOwnerUserId);

public sealed record AccessPolicySimulationResponse(bool Denied, IReadOnlyCollection<Guid> MatchingPolicyIds);

public sealed record AccessPolicySubject(
    Guid? UserId,
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> MembershipRoles,
    IReadOnlySet<Guid> DepartmentIds,
    IReadOnlySet<Guid> GroupIds,
    bool IsBootstrap);

public sealed record AccessPolicyResource(
    string ResourceType,
    Guid? ResourceId,
    string Action,
    string? RecordStatus = null,
    Guid? RecordOwnerUserId = null);
